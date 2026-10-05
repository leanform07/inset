using System.Windows;
using System.Windows.Input;
using LookUp.Hotkeys;
using LookUp.Notebook;
using LookUp.UI;

namespace LookUp;

public partial class App : Application
{
    Mutex? _singleInstance;
    GlobalHotkeyService? _hotkeys;
    PopupWindow? _popup;
    SearchWindow? _search;
    NotebookStore? _notebook;
    NotebookWindow? _notebookWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(true, @"Local\LookUp.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }

        _notebook = NotebookStore.Load(NotebookStore.DefaultPath);
        _popup = new PopupWindow(_notebook);
        _search = new SearchWindow();
        _search.LookupRequested += (query, at) => _popup.ShowLookup(query, at);

        try
        {
            await _popup.WarmUpAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't start the Microsoft Edge WebView2 runtime.\n\n{ex.Message}", "LookUp");
            Shutdown();
            return;
        }

        // Fixed for now; becomes configurable in Phase 3.
        _hotkeys = new GlobalHotkeyService();
        if (!_hotkeys.Register(ModifierKeys.Control | ModifierKeys.Alt, Key.F, ShowSearch))
            MessageBox.Show("Ctrl+Alt+F is already used by another app.", "LookUp");
        if (!_hotkeys.Register(ModifierKeys.Control | ModifierKeys.Alt, Key.N, ShowNotebook))
            MessageBox.Show("Ctrl+Alt+N is already used by another app.", "LookUp");

        ShowSearch();
    }

    void ShowSearch()
    {
        // Search first: dismissing the popup first would give the foreground away.
        _search!.ShowSearch();
        _popup!.Dismiss();
    }

    void ShowNotebook()
    {
        if (_notebookWindow == null)
        {
            _notebookWindow = new NotebookWindow(_notebook!);
            _notebookWindow.LookupRequested += (word, at) =>
                _popup!.ShowLookup(word.Word, at, Uri.TryCreate(word.SourceUrl, UriKind.Absolute, out var url) ? url : null);
            _notebookWindow.Closed += (_, _) => _notebookWindow = null;
        }
        _notebookWindow.Show();
        if (_notebookWindow.WindowState == WindowState.Minimized) _notebookWindow.WindowState = WindowState.Normal;
        _notebookWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
