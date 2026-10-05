using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LookUp.Notebook;
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
        if (Application.Current == null)
        {
            var app = new App();
            app.InitializeComponent(); // shared styles; OnStartup does not run
        }

        var dir = Path.Combine(Path.GetTempPath(), "LookUpTests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = NotebookStore.Load(Path.Combine(dir, "notebook.json"));
            Seed(store);

            _ = new PopupWindow(store);
            _ = new SearchWindow();

            var window = new NotebookWindow(store)
            {
                Left = -12000, Top = 0, Width = 1080, Height = 660,
                ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual,
            };
            window.Show();
            window.WordList.SelectedIndex = 0;
            window.UpdateLayout();
            Flush();

            Assert.Equal(3, window.WordList.Items.Count);
            Assert.Equal(Visibility.Visible, window.DetailPanel.Visibility);
            Assert.Equal("Architecture", window.CategoryEditor.Text);

            if (Environment.GetEnvironmentVariable("LOOKUP_SNAPSHOT_DIR") is { Length: > 0 } snapshotDir)
                SavePng((FrameworkElement)window.Content, Path.Combine(snapshotDir, "notebook.png"));
            window.Close();
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

    static void Flush() =>
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

    static void SavePng(FrameworkElement element, string path)
    {
        var dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap(
            (int)(element.ActualWidth * dpi.DpiScaleX), (int)(element.ActualHeight * dpi.DpiScaleY),
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(element);
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
