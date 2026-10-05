using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LookUp.Lookup;
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

    public PopupWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowEffects.MakeFloating(this);
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

    /// <summary>Shows the window immediately in a loading state, then loads the entry.</summary>
    public void ShowLookup(string query, Point topLeft)
    {
        Left = topLeft.X;
        Top = topLeft.Y;
        Load(query);
        Show();
        Activate();
    }

    public void Dismiss()
    {
        if (_state == State.Idle) return;
        _state = State.Idle;
        StopTimers();
        Hide();
        Web.Visibility = Visibility.Hidden;
        Web.CoreWebView2?.Navigate("about:blank"); // stops audio and page scripts while hidden
    }

    void Load(string query)
    {
        _query = query;
        BeginLoading();
        Web.CoreWebView2.Navigate(Cambridge.SearchUrl(query).ToString());
    }

    void BeginLoading()
    {
        _state = State.Loading;
        StopTimers();
        Web.Visibility = Visibility.Hidden;
        StatusPanel.Visibility = Visibility.Visible;
        StatusTitle.Text = _query;
        StatusMessage.Text = "Searching…";
        SuggestionsPanel.Visibility = Visibility.Collapsed;
        ActionsPanel.Visibility = Visibility.Collapsed;
        FooterNote.Text = "";
        _slowTimer.Start();
        _timeoutTimer.Start();
        Focus(); // keep Esc working while the WebView is hidden
    }

    void ShowPage(string footerNote = "")
    {
        _state = State.Showing;
        StopTimers();
        StatusPanel.Visibility = Visibility.Collapsed;
        Web.Visibility = Visibility.Visible;
        FooterNote.Text = footerNote;
        if (IsActive) Web.Focus(); // arrow keys / PageDown scroll the entry
    }

    void ShowMessage(string message, bool retry = false, IReadOnlyList<string>? suggestions = null)
    {
        _state = State.Message;
        StopTimers();
        Web.Visibility = Visibility.Hidden;
        StatusPanel.Visibility = Visibility.Visible;
        StatusTitle.Text = _query;
        StatusMessage.Text = message;
        Suggestions.ItemsSource = suggestions;
        SuggestionsPanel.Visibility = suggestions is { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
        RetryButton.Visibility = retry ? Visibility.Visible : Visibility.Collapsed;
        ActionsPanel.Visibility = Visibility.Visible;
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
            case "other":
                ShowPage();
                break;
            case "challenge":
                // Cloudflare checks a new browser profile once, then reloads the real page.
                // The page must stay visible: a hidden WebView pauses the check.
                ShowPage("One-time security check by Cloudflare…");
                break;
            case "noresult":
                var suggestions = message.RootElement.GetProperty("suggestions")
                    .EnumerateArray().Select(s => s.GetString()!).ToList();
                ShowMessage("No Cambridge Dictionary entry found.", suggestions: suggestions);
                break;
        }
    }

    void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key;
        if (key == Key.Escape)
        {
            Dismiss();
            e.Handled = true;
        }
        else if (key == Key.Enter && _state == State.Message && RetryButton.IsVisible)
        {
            Load(_query);
            e.Handled = true;
        }
    }

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
