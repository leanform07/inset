using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace WebViewSpike;

// Phase 0 spike: can a real WebView2 load Cambridge pages (Cloudflare), how fast,
// and does a reader-mode script isolate the entry? Results go to out/ next to the exe.
public partial class MainWindow : Window
{
    static readonly string[] Queries = ["resilience", "account for", "resilense"];

    const string ProbeJs = """
        (() => {
          const q = s => document.querySelectorAll(s).length;
          return JSON.stringify({
            ready: document.readyState,
            title: document.title,
            url: location.href,
            challenge: !!window._cf_chl_opt || /Ray ID/.test(document.body?.innerText ?? '') && !document.querySelector('.entry-body, .di-body'),
            entries: q('.entry-body__el'),
            ipa: q('.ipa'),
            defs: q('.def.ddef_d'),
            trans: q('.trans.dtrans'),
            examples: q('.examp'),
            audio: q('source[type="audio/mpeg"]'),
            didYouMean: /did you mean|spellcheck/i.test(location.href + ' ' + (document.querySelector('h1')?.textContent ?? ''))
          });
        })()
        """;

    // Move the first dictionary block to be the only thing on the page.
    const string ReaderJs = """
        (() => {
          const entry = document.querySelector('.pr.dictionary') || document.querySelector('.entry-body') || document.querySelector('.di-body');
          if (!entry) return 'no-entry';
          document.body.replaceChildren(entry);
          const s = document.createElement('style');
          s.textContent = `
            body{margin:0;padding:16px 20px;background:#fff;font:15px/1.5 "Segoe UI",system-ui,sans-serif}
            .hax, .dwl, .smartphone, .daccord, .lcs, .bb.hax {display:none!important}
            .def-block{margin-bottom:10px}
          `;
          document.head.appendChild(s);
          window.scrollTo(0,0);
          return 'ok';
        })()
        """;

    readonly string _outDir = Path.Combine(AppContext.BaseDirectory, "out");
    readonly StringBuilder _log = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RunAsync();
    }

    async Task RunAsync()
    {
        Directory.CreateDirectory(_outDir);
        var env = await CoreWebView2Environment.CreateAsync(
            userDataFolder: Path.Combine(AppContext.BaseDirectory, "wv2-profile"));
        var initSw = Stopwatch.StartNew();
        await Web.EnsureCoreWebView2Async(env);
        Log($"init: {initSw.ElapsedMilliseconds} ms");

        for (int round = 1; round <= 2; round++)
        {
            foreach (var q in Queries)
            {
                var url = "https://dictionary.cambridge.org/search/direct/?datasetsearch=english-chinese-traditional&q="
                          + Uri.EscapeDataString(q);
                var sw = Stopwatch.StartNew();
                var nav = await NavigateAsync(url);
                var navMs = sw.ElapsedMilliseconds;

                var probe = await ProbeAsync();
                // A managed challenge usually resolves itself in a real browser; only wait and watch, never interact.
                var t0 = Stopwatch.StartNew();
                while ((probe == "" || probe.Contains("\"challenge\":true") || probe.Contains("\"ready\":\"loading\"")) && t0.ElapsedMilliseconds < 20000)
                {
                    await Task.Delay(500);
                    probe = await ProbeAsync();
                }
                if (t0.ElapsedMilliseconds > 0) Log($"  challenge wait {t0.ElapsedMilliseconds} ms");
                var reader = JsonSerializer.Deserialize<string>(await Web.CoreWebView2.ExecuteScriptAsync(ReaderJs));
                await Task.Delay(300);
                var shot = Path.Combine(_outDir, $"r{round}-{q.Replace(' ', '_')}.png");
                await using (var fs = File.Create(shot))
                    await Web.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fs);

                Log($"round {round} | '{q}' | nav {navMs} ms ok={nav} | total {sw.ElapsedMilliseconds} ms | reader={reader} | {probe}");
            }
        }
        File.WriteAllText(Path.Combine(_outDir, "results.txt"), _log.ToString());
        Application.Current.Shutdown();
    }

    async Task<string> ProbeAsync() =>
        JsonSerializer.Deserialize<string>(await Web.CoreWebView2.ExecuteScriptAsync(ProbeJs)) ?? "";

    Task<bool> NavigateAsync(string url)
    {
        var tcs = new TaskCompletionSource<bool>();
        void Done(object? s, CoreWebView2NavigationCompletedEventArgs e)
        {
            Web.CoreWebView2.NavigationCompleted -= Done;
            tcs.TrySetResult(e.IsSuccess);
        }
        Web.CoreWebView2.NavigationCompleted += Done;
        Web.CoreWebView2.Navigate(url);
        return tcs.Task;
    }

    void Log(string line) => _log.AppendLine($"{DateTime.Now:HH:mm:ss.fff} {line}");
}
