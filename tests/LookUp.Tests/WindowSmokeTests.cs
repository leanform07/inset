using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LookUp.Notebook;
using LookUp.Settings;
using LookUp.UI;

namespace LookUp.Tests;

/// <summary>
/// Opens the windows off-screen so XAML resource mistakes fail here rather than at runtime.
/// Set LOOKUP_SNAPSHOT_DIR to also save a PNG of the notebook window.
/// </summary>
public sealed class WindowSmokeTests
{
    [Fact]
    public void Windows_load_and_the_notebook_shows_saved_words() => RunOnSta(() =>
    {
        // A plain Application with our dictionaries: creating App itself would run its OnStartup
        // (a whole LookUp instance, or a signal to the one already running) once the dispatcher runs.
        if (Application.Current == null)
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            foreach (var name in new[] { "Light", "Shared" })
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/LookUp;component/Themes/{name}.xaml"),
                });
        }

        var dir = Path.Combine(Path.GetTempPath(), "LookUpTests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = NotebookStore.Load(Path.Combine(dir, "notebook.json"));
            Seed(store);

            _ = new PopupWindow(store);
            _ = new SearchWindow();

            foreach (var theme in new[] { AppTheme.Light, AppTheme.Dark })
            {
                ThemeService.Apply(theme);
                var name = theme.ToString().ToLowerInvariant();

                var notebook = OffScreen(new NotebookWindow(store) { Width = 1080, Height = 660 });
                notebook.WordList.SelectedIndex = 0;
                Flush();
                Assert.Equal(3, notebook.WordList.Items.Count);
                Assert.Equal(Visibility.Visible, notebook.DetailPanel.Visibility);
                Assert.Equal("Architecture", notebook.CategoryEditor.Text);
                Snapshot(notebook, $"notebook-{name}.png");
                notebook.Close();

                var settings = OffScreen(new SettingsWindow(null!, new AppSettings()));
                Flush();
                Assert.Equal("Ctrl+Alt+D", settings.LookupBox.Text);
                Snapshot(settings, $"settings-{name}.png");
                settings.Close();
            }
            ThemeService.Apply(AppTheme.Light);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    });

    static void Seed(NotebookStore store)
    {
        var words = new[]
        {
            new EntrySummary("charge", "verb", "tʃɑːdʒ", "（尤指對某一服務或活動）收費，要價，開價",
                "to ask an amount of money for something, especially a service or activity",
                "How much/What do you charge for a haircut and blow-dry?", "剪髮和吹風要收多少錢？",
                "https://dictionary.cambridge.org/dictionary/english-chinese-traditional/charge"),
            new EntrySummary("account (to someone) for something", "phrasal verb", "əˈkaʊnt", "（對某人）就…作出解釋",
                "to explain the reason for something or the cause of something", "Can you account for your absence last Friday?",
                "你能解釋一下上週五你為甚麼缺勤嗎？", "https://dictionary.cambridge.org/dictionary/english-chinese-traditional/account-to-for"),
            new EntrySummary("resilience", "noun", "rɪˈzɪl.jəns", "復原力；恢復力；平復心情",
                "the ability to be happy, successful, etc. again after something difficult or bad has happened",
                "Trauma researchers emphasize the resilience of the human psyche.", "創傷研究人員強調人類心理的復原力。",
                "https://dictionary.cambridge.org/dictionary/english-chinese-traditional/resilience"),
        };
        foreach (var entry in words)
        {
            store.RecordLookup(entry.Headword);
            store.Add(entry);
        }
        var resilience = store.Find("resilience")!;
        store.SetCategory(resilience, "Architecture");
        resilience.Status = Familiarity.Learning;
        resilience.Note = "Seen in a paper on flood-resilient housing.";
        store.AddCategory("Daily life");
    }

    static T OffScreen<T>(T window) where T : Window
    {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -12000;
        window.Top = 0;
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        window.Show();
        window.UpdateLayout();
        return window;
    }

    static void Snapshot(Window window, string fileName)
    {
        if (Environment.GetEnvironmentVariable("LOOKUP_SNAPSHOT_DIR") is { Length: > 0 } dir)
            SavePng(window, Path.Combine(dir, fileName));
    }

    static void Flush() =>
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

    /// <summary>Renders the window's client area, background included (it is not part of Content).</summary>
    static void SavePng(Window window, string path)
    {
        var content = (FrameworkElement)window.Content;
        var dpi = VisualTreeHelper.GetDpi(content);
        var size = new Size(content.ActualWidth, content.ActualHeight);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(window.Background, null, new Rect(size));
            context.DrawRectangle(new VisualBrush(content), null, new Rect(size));
        }
        var bitmap = new RenderTargetBitmap(
            (int)(size.Width * dpi.DpiScaleX), (int)(size.Height * dpi.DpiScaleY),
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var file = File.Create(path);
        encoder.Save(file);
    }

    static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null) throw new Xunit.Sdk.XunitException(failure.ToString());
    }
}
