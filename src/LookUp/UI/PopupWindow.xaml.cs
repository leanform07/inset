using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using LookUp.Lookup;
using LookUp.Notebook;
using Microsoft.Web.WebView2.Core;

namespace LookUp.UI;

/// <summary>The floating result window. It is created once and reused, so the WebView stays warm.</summary>
public partial class PopupWindow : Window
{
    enum State { Idle, Loading, Showing, Message }

    readonly DispatcherTimer _slowTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    readonly DispatcherTimer _timeoutTimer = new() { Interval = TimeSpan.FromSeconds(15) };

    State _state = State.Idle;
    string _query = "";
    ulong _navigationId; // completions from older navigations are ignored

    readonly NotebookStore _notebook;
    EntrySummary? _entry;   // the entry on screen
    WordNote? _editing;     // the saved word open in the note panel

    internal PopupWindow(NotebookStore notebook)
    {
        _notebook = notebook;
        InitializeComponent();
        CategoryBox.ItemsSource = notebook.Categories;
        SourceInitialized += (_, _) =>
        {
            WindowEffects.MakeFloating(this);
            WindowEffects.RemoveMaximize(this);
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle).AddHook(WndProc);
        };
        StateChanged += (_, _) => { if (WindowState != WindowState.Normal) WindowState = WindowState.Normal; };
        Deactivated += (_, _) => Dismiss();
        PreviewKeyDown += OnPreviewKeyDown;

        _slowTimer.Tick += (_, _) =>
        {
            _slowTimer.Stop();
            if (_state == State.Loading) StatusMessage.Text = "Still searching…";
        };
        _timeoutTimer.Tick += (_, _) =>
        {
            _timeoutTimer.Stop();
            if (_state != State.Loading) return;
            Web.CoreWebView2.Stop();
            ShowMessage("Cambridge Dictionary is taking too long to respond.", retry: true);
        };
    }

    /// <summary>Creates the WebView while the window is off-screen, so the first lookup does not pay for it.</summary>
    public async Task WarmUpAsync()
    {
        Left = -10000;
        ShowActivated = false;
        Show();

        var dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LookUp", "WebView2");
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: dataFolder);
        await Web.EnsureCoreWebView2Async(environment);

        var core = Web.CoreWebView2;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        await core.AddScriptToExecuteOnDocumentCreatedAsync(Cambridge.ReaderScript());

        // reader.css loads the app's fonts from /__lookup/ on the Cambridge origin, so no
        // cross-origin rules apply; the app answers those requests itself.
        core.AddWebResourceRequestedFilter($"https://{Cambridge.Host}/__lookup/*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += OnAppResourceRequested;
        ApplyTheme();
        ThemeService.Changed += ApplyTheme;
        core.NavigationStarting += OnNavigationStarting;
        core.NavigationCompleted += OnNavigationCompleted;
        core.WebMessageReceived += OnWebMessageReceived;
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri)) OpenInBrowser(uri);
        };

        Hide();
        ShowActivated = true;
    }

    static readonly HashSet<string> ServedFonts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Geist-Regular.ttf", "Geist-Medium.ttf", "GeistMono-Regular.ttf",
    };

    void OnAppResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var environment = Web.CoreWebView2.Environment;
        var name = Path.GetFileName(new Uri(e.Request.Uri).AbsolutePath);
        if (!ServedFonts.Contains(name))
        {
            e.Response = environment.CreateWebResourceResponse(null, 404, "Not Found", "");
            return;
        }
        var font = Application.GetResourceStream(new Uri($"pack://application:,,,/LookUp;component/Assets/Fonts/{name}"));
        e.Response = environment.CreateWebResourceResponse(font.Stream, 200, "OK",
            "Content-Type: font/ttf\r\nCache-Control: max-age=31536000");
    }

    /// <summary>The entry page follows the app theme through prefers-color-scheme (see reader.css).</summary>
    void ApplyTheme()
    {
        if (Web.CoreWebView2 is not { } core) return;
        core.Profile.PreferredColorScheme = ThemeService.IsDark
            ? CoreWebView2PreferredColorScheme.Dark
            : CoreWebView2PreferredColorScheme.Light;
        var surface = (System.Windows.Media.Color)FindResource("GroundWarmColor");
        Web.DefaultBackgroundColor = System.Drawing.Color.FromArgb(surface.R, surface.G, surface.B);
    }

    /// <summary>
    /// Shows the window immediately in a loading state, then loads the entry.
    /// A known entry URL (from the notebook) is opened directly instead of searching.
    /// </summary>
    internal void ShowLookup(string query, ScreenPlacement.Anchor anchor, Uri? entryUrl = null)
    {
        Load(query, entryUrl);
        Present(anchor);
    }

    /// <summary>Shows a short message instead of an entry, e.g. when no text was selected.</summary>
    internal void ShowNotice(string title, string message, ScreenPlacement.Anchor anchor)
    {
        _query = title;
        CloseNotePanel();
        _entry = null;
        UpdateNotebookButton();
        Web.CoreWebView2?.Navigate("about:blank");
        ShowMessage(message, actions: false);
        Present(anchor);
    }

    void Present(ScreenPlacement.Anchor anchor)
    {
        ScreenPlacement.Place(this, anchor);
        Show();
        ScreenPlacement.Place(this, anchor); // again, in case showing on another monitor changed its DPI
        Activate();
    }

    public void Dismiss()
    {
        if (_state == State.Idle) return;
        CloseNotePanel();
        _state = State.Idle;
        StopTimers();
        Hide();
        Web.Visibility = Visibility.Hidden;
        Web.CoreWebView2?.Navigate("about:blank"); // stops audio and page scripts while hidden
    }

    void Load(string query, Uri? entryUrl = null)
    {
        _query = query;
        BeginLoading();
        Web.CoreWebView2.Navigate((entryUrl ?? Cambridge.SearchUrl(query)).ToString());
    }

    void BeginLoading()
    {
        CloseNotePanel();
        _entry = null;
        UpdateNotebookButton();
        _state = State.Loading;
        StopTimers();
        Web.Visibility = Visibility.Hidden;
        StatusPanel.Visibility = Visibility.Visible;
        StatusTitle.Text = _query;
        StatusTitle.Visibility = Visibility.Visible;
        StatusMessage.Text = "Searching…";
        OpenInCambridgeButton.Visibility = Visibility.Visible;
        SuggestionsPanel.Visibility = Visibility.Collapsed;
        ActionsPanel.Visibility = Visibility.Collapsed;
        SetTokens();
        _slowTimer.Start();
        _timeoutTimer.Start();
        Focus(); // keep Esc working while the WebView is hidden
    }

    void ShowPage(string? source = null, string count = "")
    {
        _state = State.Showing;
        StopTimers();
        StatusPanel.Visibility = Visibility.Collapsed;
        Web.Visibility = Visibility.Visible;
        SetTokens(source, count);
        if (IsActive) Web.Focus(); // arrow keys / PageDown scroll the entry
    }

    /// <summary>The token bar: which dictionary the entry is from, and how often the word has come up.</summary>
    void SetTokens(string? source = null, string count = "")
    {
        SourceToken.Text = source ?? "EN · 中文";
        CountToken.Text = count;
    }

    void ShowMessage(string message, bool retry = false, IReadOnlyList<string>? suggestions = null, bool actions = true)
    {
        _state = State.Message;
        StopTimers();
        Web.Visibility = Visibility.Hidden;
        StatusPanel.Visibility = Visibility.Visible;
        StatusTitle.Text = _query;
        StatusTitle.Visibility = _query.Length > 0 ? Visibility.Visible : Visibility.Collapsed; // notices have no word
        StatusMessage.Text = message;
        Suggestions.ItemsSource = suggestions;
        SuggestionsPanel.Visibility = suggestions is { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
        RetryButton.Visibility = retry ? Visibility.Visible : Visibility.Collapsed;
        ActionsPanel.Visibility = actions ? Visibility.Visible : Visibility.Collapsed;
        OpenInCambridgeButton.Visibility = _query.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        Focus();
    }

    void StopTimers()
    {
        _slowTimer.Stop();
        _timeoutTimer.Stop();
    }

    void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) || uri.Scheme == "about") return;

        if (!Cambridge.IsDictionaryHost(uri))
        {
            e.Cancel = true;
            OpenInBrowser(uri);
            return;
        }

        _navigationId = e.NavigationId;
        if (e.IsUserInitiated && _state == State.Showing)
        {
            // A word inside the entry was clicked: look that up in place.
            _query = Cambridge.QueryFromEntryUrl(uri);
            BeginLoading();
        }
    }

    void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.NavigationId != _navigationId || _state != State.Loading) return;

        if (!e.IsSuccess && IsConnectionProblem(e.WebErrorStatus))
            ShowMessage("Couldn't reach Cambridge Dictionary. Check your internet connection.", retry: true);
        else
            ShowPage(); // reader.js did not recognise the page; show it as it is
    }

    void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        using var message = JsonDocument.Parse(e.WebMessageAsJson);
        var type = message.RootElement.GetProperty("type").GetString();

        if (_state is State.Idle or State.Message) return;

        switch (type)
        {
            case "entry":
                _entry = ParseSummary(message.RootElement.GetProperty("summary"));
                if (_entry.Headword.Length == 0) _entry = _entry with { Headword = _query };
                var lookups = _notebook.RecordLookup(_entry.Headword);
                // Cambridge's own search falls back to its English-only dictionary when the
                // English–Chinese one lacks the word; say so, or the missing Chinese looks like a bug.
                ShowPage(
                    Cambridge.IsEnglishOnly(_entry.SourceUrl) ? "EN ONLY · NO CHINESE" : null,
                    lookups.Count >= 2 ? $"LOOKED UP {lookups.Count:00}" : "");
                UpdateNotebookButton();
                UpdateSeal(animate: false);
                break;
            case "other":
                ShowPage();
                break;
            case "challenge":
                // Cloudflare checks a new browser profile once, then reloads the real page.
                // The page must stay visible: a hidden WebView pauses the check.
                ShowPage("ONE-TIME SECURITY CHECK · CLOUDFLARE");
                break;
            case "noresult":
                var suggestions = message.RootElement.GetProperty("suggestions")
                    .EnumerateArray().Select(s => s.GetString()!).ToList();
                ShowMessage("No Cambridge Dictionary entry found.", suggestions: suggestions);
                break;
        }
    }

    // ── Moving and sizing ─────────────────────────────────────

    public const double DefaultWidth = 440, DefaultHeight = 560;

    /// <summary>Raised when the user resizes the window or resets it; null means the default size.</summary>
    internal event Action<Size?>? SizeChosen;

    Size? _chosenSize;

    /// <summary>Applies a remembered size (null for the default).</summary>
    internal void UseSize(Size? size)
    {
        _chosenSize = size;
        Width = Math.Max(MinWidth, size?.Width ?? DefaultWidth);
        Height = Math.Max(MinHeight, size?.Height ?? DefaultHeight);
        ResetSizeButton.Visibility = size == null ? Visibility.Collapsed : Visibility.Visible;
    }

    void OnTokenBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) ResetSize();
        else if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    void OnResetSizeClick(object sender, RoutedEventArgs e)
    {
        ResetSize();
        if (_state == State.Showing) Web.Focus();
        else Focus();
    }

    void ResetSize()
    {
        if (_chosenSize == null) return;
        UseSize(null);
        SizeChosen?.Invoke(null);
    }

    const int WM_EXITSIZEMOVE = 0x0232;

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_EXITSIZEMOVE) OnSizeMoveEnded();
        return IntPtr.Zero;
    }

    /// <summary>After a drag on the edges, remembers the new size (a plain move changes nothing).</summary>
    void OnSizeMoveEnded()
    {
        var size = new Size(Math.Round(ActualWidth), Math.Round(ActualHeight));
        var current = _chosenSize ?? new Size(DefaultWidth, DefaultHeight);
        if (size == current) return;
        var chosen = size == new Size(DefaultWidth, DefaultHeight) ? (Size?)null : size;
        UseSize(chosen);
        SizeChosen?.Invoke(chosen);
    }

    void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key;
        if (key == Key.Escape)
        {
            if (_editing != null) CloseNotePanel();
            else Dismiss();
            e.Handled = true;
        }
        else if (key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OnNotebookButtonClick(this, e);
            e.Handled = true;
        }
        else if (key == Key.Enter && _editing != null)
        {
            CloseNotePanel();
            e.Handled = true;
        }
        else if (key == Key.Enter && _state == State.Message && RetryButton.IsVisible)
        {
            Load(_query);
            e.Handled = true;
        }
    }

    // ── Notebook ──────────────────────────────────────────────

    void OnNotebookButtonClick(object sender, RoutedEventArgs e)
    {
        if (_entry == null || _state != State.Showing) return;
        if (_editing != null)
        {
            CloseNotePanel();
            return;
        }

        // Saving is immediate; the panel only offers optional details.
        var existing = _notebook.Find(_entry.Headword);
        _editing = existing ?? _notebook.Add(_entry);
        if (existing == null) UpdateSeal(animate: true); // the stamp: once, when the word is first saved
        CategoryBox.Text = _editing.Category;
        StatusNew.IsChecked = _editing.Status == Familiarity.New;
        StatusLearning.IsChecked = _editing.Status == Familiarity.Learning;
        StatusKnown.IsChecked = _editing.Status == Familiarity.Known;
        NoteBox.Text = _editing.Note;
        NotePanel.Visibility = Visibility.Visible;
        UpdateNotebookButton();
        CategoryBox.Focus();
    }

    void OnNoteDoneClick(object sender, RoutedEventArgs e) => CloseNotePanel();

    void OnNoteRemoveClick(object sender, RoutedEventArgs e)
    {
        if (_editing == null) return;
        _notebook.Remove(_editing);
        _editing = null;
        NotePanel.Visibility = Visibility.Collapsed;
        UpdateNotebookButton();
        UpdateSeal(animate: false);
        if (_state == State.Showing) Web.Focus();
    }

    /// <summary>Saves what is in the note panel and closes it.</summary>
    void CloseNotePanel()
    {
        if (_editing == null) return;
        _notebook.SetCategory(_editing, CategoryBox.Text);
        _editing.Status = StatusKnown.IsChecked == true ? Familiarity.Known
                        : StatusLearning.IsChecked == true ? Familiarity.Learning
                        : Familiarity.New;
        _editing.Note = NoteBox.Text.Trim();
        _editing = null;
        NotePanel.Visibility = Visibility.Collapsed;
        UpdateNotebookButton();
        UpdateSeal(animate: false); // the category is written on the seal
        if (_state == State.Showing && IsActive) Web.Focus();
    }

    /// <summary>Filled ink "Add to notebook" until the word is saved, then an outline naming its category.</summary>
    void UpdateNotebookButton()
    {
        var note = _entry == null ? null : _notebook.Find(_entry.Headword);
        NotebookButton.Style = (Style)FindResource(note == null ? "PrimaryButton" : "SecondaryButton");
        NotebookIcon.Text = note == null ? "\uE734" : "\uE735"; // FavoriteStar / FavoriteStarFill
        NotebookLabel.Text = note == null ? "Add to notebook"
                           : note.Category.Length > 0 ? note.Category
                           : "In notebook";
        NotebookButton.Visibility = _entry != null ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Stamps (or clears) the seal on the entry page; reader.js draws it.</summary>
    void UpdateSeal(bool animate)
    {
        if (_state != State.Showing || _entry == null || Web.CoreWebView2 == null) return;
        var note = _notebook.Find(_entry.Headword);
        var seal = note == null ? "null" : JsonSerializer.Serialize(new
        {
            category = note.Category.Length > 0 ? note.Category : "Uncategorized",
            date = note.AddedAt.ToString("yyyy.MM.dd"),
            count = note.LookupCount,
            animate,
        });
        _ = Web.CoreWebView2.ExecuteScriptAsync($"window.__lookupSeal && window.__lookupSeal({seal})");
    }

    static EntrySummary ParseSummary(JsonElement s)
    {
        string Get(string name) => s.TryGetProperty(name, out var v) ? v.GetString() ?? "" : "";
        return new EntrySummary(Get("headword"), Get("partOfSpeech"), Get("ipa"), Get("chinese"),
            Get("definition"), Get("example"), Get("exampleChinese"), Get("sourceUrl"));
    }

    // ── Message actions ───────────────────────────────────────

    void OnSuggestionClick(object sender, RoutedEventArgs e) => Load((string)((FrameworkElement)sender).DataContext);

    void OnRetryClick(object sender, RoutedEventArgs e) => Load(_query);

    void OnWebSearchClick(object sender, RoutedEventArgs e) => OpenInBrowser(Cambridge.WebSearchUrl(_query));

    void OnOpenInCambridgeClick(object sender, RoutedEventArgs e)
    {
        var current = Web.CoreWebView2?.Source;
        OpenInBrowser(_state == State.Showing && Uri.TryCreate(current, UriKind.Absolute, out var page)
            ? page
            : Cambridge.SearchUrl(_query));
    }

    static bool IsConnectionProblem(CoreWebView2WebErrorStatus status) => status is
        CoreWebView2WebErrorStatus.Timeout or
        CoreWebView2WebErrorStatus.HostNameNotResolved or
        CoreWebView2WebErrorStatus.CannotConnect or
        CoreWebView2WebErrorStatus.Disconnected or
        CoreWebView2WebErrorStatus.ConnectionAborted or
        CoreWebView2WebErrorStatus.ConnectionReset or
        CoreWebView2WebErrorStatus.ServerUnreachable;

    static void OpenInBrowser(Uri uri)
    {
        if (uri.Scheme is "http" or "https")
            Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
    }
}
