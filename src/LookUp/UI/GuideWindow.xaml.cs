using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using LookUp.Settings;
using Microsoft.Web.WebView2.Core;

namespace LookUp.UI;

/// <summary>
/// Shows the how-to guide in a window of the app's own. Handing the page to the default browser
/// proved unreliable: a browser whose windows are all minimised can take the file and show nothing.
/// </summary>
public partial class GuideWindow : Window
{
    // .invalid never resolves, so nothing here can reach the network; the app answers every request.
    const string Host = "guide.inset.invalid";

    static readonly HashSet<string> ServedFonts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Geist-Regular.ttf", "GeistMono-Regular.ttf",
    };

    readonly TaskCompletionSource<bool> _shown = new();

    public GuideWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
        Closed += (_, _) => ThemeService.Changed -= ApplyTheme;
    }

    /// <summary>Same profile folder (and options) as the popup, so both share one WebView2 browser process. Tests use their own.</summary>
    internal string UserDataFolder { get; init; } = Path.Combine(AppFolders.Local, "WebView2");

    /// <summary>Completes once the guide page has loaded (false if it failed).</summary>
    internal Task<bool> Shown => _shown.Task;

    async Task LoadAsync()
    {
        ApplyBackground();
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: UserDataFolder);
        await Web.EnsureCoreWebView2Async(environment);

        var core = Web.CoreWebView2;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.AddWebResourceRequestedFilter($"https://{Host}/*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += OnResourceRequested;
        core.NavigationStarting += (_, e) =>
        {
            if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) && uri.Host != Host)
            {
                e.Cancel = true;
                if (uri.Scheme is "http" or "https")
                    Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
            }
        };
        core.NavigationCompleted += (_, e) => _shown.TrySetResult(e.IsSuccess);
        ApplyTheme();
        ThemeService.Changed += ApplyTheme;
        core.Navigate($"https://{Host}/guide.html");
    }

    void OnResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var environment = Web.CoreWebView2.Environment;
        var path = new Uri(e.Request.Uri).AbsolutePath;
        var name = Path.GetFileName(path);

        if (path == "/guide.html")
        {
            var html = new MemoryStream(Encoding.UTF8.GetBytes(UserGuide.Html()));
            e.Response = environment.CreateWebResourceResponse(html, 200, "OK", "Content-Type: text/html; charset=utf-8");
        }
        else if (path.StartsWith("/fonts/", StringComparison.Ordinal) && ServedFonts.Contains(name))
        {
            var font = Application.GetResourceStream(new Uri($"pack://application:,,,/Inset;component/Assets/Fonts/{name}"));
            e.Response = environment.CreateWebResourceResponse(font.Stream, 200, "OK", "Content-Type: font/ttf");
        }
        else
        {
            e.Response = environment.CreateWebResourceResponse(null, 404, "Not Found", "");
        }
    }

    /// <summary>The page follows the app theme through prefers-color-scheme, like the entry page.</summary>
    void ApplyTheme()
    {
        if (Web.CoreWebView2 is not { } core) return;
        core.Profile.PreferredColorScheme = ThemeService.IsDark
            ? CoreWebView2PreferredColorScheme.Dark
            : CoreWebView2PreferredColorScheme.Light;
        ApplyBackground();
    }

    void ApplyBackground()
    {
        var surface = (System.Windows.Media.Color)FindResource("GroundWarmColor");
        Web.DefaultBackgroundColor = System.Drawing.Color.FromArgb(surface.R, surface.G, surface.B);
    }
}
