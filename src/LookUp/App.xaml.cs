using System.IO;
using System.Windows;
using LookUp.Hotkeys;
using LookUp.Lookup;
using LookUp.Notebook;
using LookUp.Selection;
using LookUp.Settings;
using LookUp.UI;

namespace LookUp;

public partial class App : Application
{
    internal const string LookupSelectionAction = "lookup", SearchAction = "search", NotebookAction = "notebook";
    static readonly string[] Actions = [LookupSelectionAction, SearchAction, NotebookAction];

    const string InstanceName = @"Local\Inset.SingleInstance";
    const string ShowSearchSignalName = @"Local\Inset.ShowSearch";

    Mutex? _singleInstance;
    EventWaitHandle? _showSearchSignal;
    AppSettings _settings = new();
    GlobalHotkeyService? _hotkeys;
    bool _hotkeysPaused;
    TrayIcon? _tray;
    PopupWindow? _popup;
    SearchWindow? _search;
    NotebookStore? _notebook;
    NotebookWindow? _notebookWindow;
    SettingsWindow? _settingsWindow;
    bool _readingSelection;
    AiConclusions? _conclusions;
    ClipboardWatcher? _clipboard;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // A second launch asks the running copy to open its search box, then exits.
        _singleInstance = new Mutex(true, InstanceName, out bool isFirst);
        _showSearchSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSearchSignalName);
        if (!isFirst)
        {
            _showSearchSignal.Set();
            Shutdown();
            return;
        }
        ThreadPool.RegisterWaitForSingleObject(_showSearchSignal,
            (_, _) => Dispatcher.BeginInvoke(ShowSearch), null, Timeout.Infinite, executeOnlyOnce: false);

        AppFolders.MoveFromFormerName();
        StartupRegistration.MoveFromFormerName();
        _settings = AppSettings.Load(AppSettings.DefaultPath);
        ThemeService.Apply(_settings.Theme);

        _notebook = OpenNotebook(NotebookStore.DefaultPath);
        _popup = new PopupWindow(_notebook);
        _popup.UseSize(_settings.PopupSize);
        _popup.Assistant = _settings.AiAssistant;
        _conclusions = new AiConclusions(_notebook);
        _clipboard = new ClipboardWatcher(OnClipboardText);
        _popup.QuestionAsked += entry => ExpectConclusion(entry.Headword, entry);
        _popup.SizeChosen += size =>
        {
            _settings.PopupSize = size;
            _settings.Save(AppSettings.DefaultPath);
        };
        _search = new SearchWindow();
        _search.LookupRequested += (query, anchor) => _popup.ShowLookup(query, anchor);

        try
        {
            await _popup.WarmUpAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't start the Microsoft Edge WebView2 runtime.\n\n{ex.Message}", "Inset");
            Shutdown();
            return;
        }

        _tray = new TrayIcon(ShowSearch, ShowNotebook, ShowSettings, Shutdown);
        _hotkeys = new GlobalHotkeyService();
        var taken = Actions.Where(a => !_hotkeys.Set(a, HotkeyFor(a), HandlerFor(a))).Select(a => HotkeyFor(a).ToString()).ToList();
        UpdateShortcutHints();

        if (taken.Count > 0)
        {
            _tray.ShowMessage("Some shortcuts are taken",
                $"{string.Join(", ", taken)} {(taken.Count == 1 ? "is" : "are")} used by another app. Choose another in Settings.");
        }
        else if (!_settings.WelcomeShown)
        {
            _tray.ShowMessage("Inset is running",
                $"Select a word in any app and press {_settings.LookupSelection}. Inset stays in the notification area.");
            _settings.WelcomeShown = true;
            _settings.Save(AppSettings.DefaultPath);
        }

        if (!e.Args.Contains(StartupRegistration.BackgroundArgument)) ShowSearch();
    }

    /// <summary>
    /// Loads the notebook, offering the latest backup if words have gone missing since the last save,
    /// and keeps the backups and the recorded word count up to date from then on.
    /// </summary>
    static NotebookStore OpenNotebook(string path)
    {
        var backups = new NotebookBackups(NotebookBackups.DefaultFolder);
        var wordCount = new NotebookWordCount();

        var store = NotebookStore.Load(path);
        if (NotebookRecovery.Check(store, path, wordCount.Value, backups) is { } problem)
        {
            if (problem.Backup == null)
                MessageBox.Show(problem.Message, "Inset", MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (MessageBox.Show(problem.Message, "Inset", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    store = NotebookRecovery.Restore(path, problem.Backup);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    MessageBox.Show($"Couldn't restore the backup.\n\n{ex.Message}\n\nIt is still at {problem.Backup.Path}.",
                        "Inset", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Asked at most once: from here on, the notebook as it now is counts as the one to protect.
        wordCount.Value = store.Words.Count;
        backups.Take(path);
        store.Changed += () =>
        {
            wordCount.Value = store.Words.Count;
            backups.Take(path);
        };
        return store;
    }

    // ── Actions ───────────────────────────────────────────────

    void ShowSearch()
    {
        // Search first: dismissing the popup first would give the foreground away.
        _search!.ShowSearch();
        _popup!.Dismiss();
    }

    /// <summary>Looks up whatever is selected in the app in front.</summary>
    async void LookUpSelection()
    {
        if (_readingSelection) return;
        _readingSelection = true;
        try
        {
            var anchor = ScreenPlacement.Anchor.Cursor(ScreenPlacement.Cursor());

            // Read before showing anything: the popup taking focus would change what Ctrl+C copies.
            var selection = await SelectionReader.ReadAsync();
            if (selection.Failure != SelectionFailure.None)
            {
                _popup!.ShowNotice("", DescribeFailure(selection.Failure), anchor);
                return;
            }

            var query = QueryNormalizer.Normalize(selection.Text);
            var notice = query.Problem switch
            {
                QueryProblem.None => null,
                QueryProblem.TooLong => $"Selection is too long for dictionary lookup.\nSelect a word or a phrase of up to {QueryNormalizer.MaxWords} words.",
                _ => $"Select an English word or phrase, then press {_settings.LookupSelection}.",
            };
            if (notice == null) _popup!.ShowLookup(query.Text, anchor);
            else _popup!.ShowNotice("", notice, anchor);
        }
        finally
        {
            _readingSelection = false;
        }
    }

    string DescribeFailure(SelectionFailure failure) => failure switch
    {
        SelectionFailure.Elevated => $"Can't read text from an app running as administrator.\nPress {_settings.Search} to type the word instead.",
        SelectionFailure.Terminal => $"Can't read the selection in a terminal.\nPress {_settings.Search} to type the word instead.",
        SelectionFailure.KeysHeld => $"Release the keys after pressing {_settings.LookupSelection}.",
        _ => $"Couldn't get the selected text.\nSelect a word first, or press {_settings.Search} to type it.",
    };

    void ShowNotebook()
    {
        if (_notebookWindow == null)
        {
            _notebookWindow = new NotebookWindow(_notebook!) { Assistant = _settings.AiAssistant };
            _notebookWindow.LookupRequested += (word, anchor) =>
                _popup!.ShowLookup(word.Word, anchor, Uri.TryCreate(word.SourceUrl, UriKind.Absolute, out var url) ? url : null);
            _notebookWindow.QuestionAsked += word => ExpectConclusion(word, null);
            _notebookWindow.Closed += (_, _) => _notebookWindow = null;
        }
        Bring(_notebookWindow);
    }

    /// <summary>The clipboard is watched only while a question's conclusion may still be copied.</summary>
    void ExpectConclusion(string word, EntrySummary? entry)
    {
        _conclusions!.Expect(word, entry);
        _clipboard!.Start();
    }

    void OnClipboardText(string text)
    {
        if (_conclusions!.Accept(text) is { } note)
            _tray?.ShowMessage("Saved to your notebook", $"The AI’s conclusion is now in the note for “{note.Word}”.");
        if (!_conclusions.IsExpecting) _clipboard!.Stop();
    }

    void ShowSettings()
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow(this, _settings);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }
        Bring(_settingsWindow);
    }

    static void Bring(Window window)
    {
        window.Show();
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        window.Activate();
    }

    // ── Settings, called by SettingsWindow ────────────────────

    Hotkey HotkeyFor(string action) => action switch
    {
        LookupSelectionAction => _settings.LookupSelection,
        SearchAction => _settings.Search,
        _ => _settings.Notebook,
    };

    Action HandlerFor(string action) => action switch
    {
        LookupSelectionAction => LookUpSelection,
        SearchAction => ShowSearch,
        _ => ShowNotebook,
    };

    static string Describe(string action) => action switch
    {
        LookupSelectionAction => "Look up selected text",
        SearchAction => "Search box",
        _ => "Notebook",
    };

    /// <summary>While a shortcut is being recorded, its keys must reach the Settings window.</summary>
    internal void PauseHotkeys(bool pause)
    {
        _hotkeysPaused = pause;
        if (pause) _hotkeys!.Suspend();
        else _hotkeys!.Resume();
    }

    /// <returns>null on success, otherwise why the shortcut can't be used.</returns>
    internal string? TrySetHotkey(string action, Hotkey hotkey)
    {
        var other = Actions.FirstOrDefault(a => a != action && HotkeyFor(a) == hotkey);
        return other != null ? $"Already used for “{Describe(other)}”." : ApplyHotkey(action, hotkey);
    }

    string? ApplyHotkey(string action, Hotkey hotkey)
    {
        // Registration can only be tested with hotkeys active, even while a box is recording.
        if (_hotkeysPaused) _hotkeys!.Resume();
        var ok = _hotkeys!.Set(action, hotkey, HandlerFor(action));
        if (_hotkeysPaused) _hotkeys.Suspend();
        if (!ok) return "Another app is already using this shortcut.";

        switch (action)
        {
            case LookupSelectionAction: _settings.LookupSelectionHotkey = hotkey.ToString(); break;
            case SearchAction: _settings.SearchHotkey = hotkey.ToString(); break;
            default: _settings.NotebookHotkey = hotkey.ToString(); break;
        }
        _settings.Save(AppSettings.DefaultPath);
        UpdateShortcutHints();
        return null;
    }

    /// <returns>Problems by action, for defaults another app has taken.</returns>
    internal Dictionary<string, string> ResetHotkeys()
    {
        // Clear first, so defaults that had been swapped between actions don't collide.
        foreach (var action in Actions) _hotkeys!.Clear(action);

        var problems = new Dictionary<string, string>();
        foreach (var (action, hotkey) in Actions.Zip([AppSettings.DefaultLookupSelection, AppSettings.DefaultSearch, AppSettings.DefaultNotebook]))
            if (ApplyHotkey(action, hotkey) is { } problem) problems[action] = problem;
        return problems;
    }

    internal void SetTheme(AppTheme theme)
    {
        _settings.Theme = theme;
        _settings.Save(AppSettings.DefaultPath);
        ThemeService.Apply(theme);
    }

    internal void SetAssistant(AiAssistant assistant)
    {
        _settings.AiAssistant = assistant;
        _settings.Save(AppSettings.DefaultPath);
        _popup!.Assistant = assistant;
        if (_notebookWindow != null) _notebookWindow.Assistant = assistant;
    }

    void UpdateShortcutHints()
    {
        _tray?.SetShortcuts(_settings.Search.ToString(), _settings.Notebook.ToString());
        _search?.SetShortcuts(_settings.LookupSelection.ToString(), _settings.Notebook.ToString());
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _hotkeys?.Dispose();
        _clipboard?.Dispose();
        _showSearchSignal?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
