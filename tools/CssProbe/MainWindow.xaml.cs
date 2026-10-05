using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace WebViewSpike;

// Renders entries with the app's real reader.js/css off-screen, without taking focus, and captures them.
// Args: <outDir> <query...>. Env PROBE_DARK=1 for the dark scheme, PROBE_SEAL=1 to stamp the seal.
public partial class MainWindow : Window
{
    const string Src = @"D:\Projects\Dictionary\src\LookUp\";

    public MainWindow()
    {
        InitializeComponent();
        Left = -3000; Top = 0; ShowActivated = false; ShowInTaskbar = false;
        Width = 438; Height = 478 + 39; // popup content area plus this window's own title bar
        Loaded += async (_, _) => await RunAsync();
    }

    async Task RunAsync()
    {
        var css = File.ReadAllText(Src + @"Web\reader.css");
        var js = File.ReadAllText(Src + @"Web\reader.js").Replace("__LU_CSS__", System.Text.Json.JsonSerializer.Serialize(css));
        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(AppContext.BaseDirectory, "probe-profile"));
        await Web.EnsureCoreWebView2Async(env);
        var core = Web.CoreWebView2;
        var dark = Environment.GetEnvironmentVariable("PROBE_DARK") == "1";
        core.Profile.PreferredColorScheme = dark ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
        Web.DefaultBackgroundColor = dark ? System.Drawing.Color.FromArgb(0x1B, 0x1A, 0x1B) : System.Drawing.Color.FromArgb(0xFA, 0xF6, 0xF5);
        await core.AddScriptToExecuteOnDocumentCreatedAsync(js);
        core.AddWebResourceRequestedFilter("https://dictionary.cambridge.org/__lookup/*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += (_, e) =>
        {
            var file = Path.Combine(Src, "Assets", "Fonts", Path.GetFileName(new Uri(e.Request.Uri).AbsolutePath));
            e.Response = File.Exists(file)
                ? core.Environment.CreateWebResourceResponse(File.OpenRead(file), 200, "OK", "Content-Type: font/ttf")
                : core.Environment.CreateWebResourceResponse(null, 404, "Not Found", "");
        };

        var outDir = Environment.GetCommandLineArgs()[1];
        var suffix = dark ? "-dark" : "-light";
        foreach (var q in Environment.GetCommandLineArgs().Skip(2))
        {
            var tcs = new TaskCompletionSource<string>();
            void OnMsg(object? s, CoreWebView2WebMessageReceivedEventArgs e) { if (!e.WebMessageAsJson.Contains("\"type\":\"challenge\"")) tcs.TrySetResult(e.WebMessageAsJson); }
            core.WebMessageReceived += OnMsg;
            core.Navigate("https://dictionary.cambridge.org/search/direct/?datasetsearch=english-chinese-traditional&q=" + Uri.EscapeDataString(q));
            var msg = await Task.WhenAny(tcs.Task, Task.Delay(20000)) == tcs.Task ? tcs.Task.Result : "timeout";
            core.WebMessageReceived -= OnMsg;
            if (Environment.GetEnvironmentVariable("PROBE_SEAL") == "1")
                await core.ExecuteScriptAsync("window.__lookupSeal && window.__lookupSeal({category:'Architecture',date:'2026.10.05',count:3,animate:false})");
            await Task.Delay(1600);
            await using (var fs = File.Create(Path.Combine(outDir, q.Replace(' ', '_') + suffix + ".png")))
                await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fs);
            File.AppendAllText(Path.Combine(outDir, "log.txt"), $"{q}{suffix}: {msg[..Math.Min(80, msg.Length)]}\n");
        }
        Application.Current.Shutdown();
    }
}
