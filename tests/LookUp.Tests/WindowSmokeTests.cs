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
                    Source = new Uri($"pack://application:,,,/Inset;component/Themes/{name}.xaml"),
                });
        }

        var dir = Path.Combine(Path.GetTempPath(), "LookUpTests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = NotebookStore.Load(Path.Combine(dir, "notebook.json"));
            Seed(store);


            foreach (var theme in new[] { AppTheme.Light, AppTheme.Dark })
            {
                ThemeService.Apply(theme);
                var name = theme.ToString().ToLowerInvariant();

                var notebook = OffScreen(new NotebookWindow(store) { Width = 1120, Height = 700 });
                notebook.WordList.SelectedIndex = 0;
                Flush();
                Assert.Equal(3, notebook.WordList.Items.Count);
                Assert.Equal(Visibility.Visible, notebook.DetailPanel.Visibility);
                Assert.Equal("Architecture", notebook.CategoryEditor.Text);
                Snapshot(notebook, $"notebook-{name}.png");
                notebook.Close();

                var empty = OffScreen(new NotebookWindow(NotebookStore.Load(Path.Combine(dir, $"empty-{name}.json"))) { Width = 1120, Height = 700 });
                Flush();
                Assert.Equal(Visibility.Visible, empty.EmptyText.Visibility);
                Snapshot(empty, $"notebook-empty-{name}.png");
                empty.Close();

                var settings = OffScreen(new SettingsWindow(null!, new AppSettings()));
                Flush();
                Assert.Equal("Ctrl+Alt+D", settings.LookupBox.Text);
                Snapshot(settings, $"settings-{name}.png");
                settings.Close();

                var search = OffScreen(new SearchWindow());
                search.SetShortcuts("Ctrl+Alt+D", "Ctrl+Alt+N");
                search.Input.Text = "resilience";
                // As it reopens: the last word selected. Off screen the box has no focus, so draw the selection anyway.
                search.Input.IsInactiveSelectionHighlightEnabled = true;
                search.Input.SelectAll();
                Flush();
                Snapshot(search, $"search-{name}.png");
                search.Close();

                // The popup's own chrome around a no-result message, with the notebook panel open.
                // (The entry itself is a WebView page; reader.css is checked separately.)
                var popup = OffScreen(new PopupWindow(store));
                popup.StatusTitle.Text = "resilense";
                popup.StatusMessage.Text = "No Cambridge Dictionary entry found.";
                popup.Suggestions.ItemsSource = new[] { "resilience", "resiliency", "residence" };
                popup.SuggestionsPanel.Visibility = Visibility.Visible;
                popup.ActionsPanel.Visibility = Visibility.Visible;
                popup.NotePanel.Visibility = Visibility.Visible;
                popup.StatusLearning.IsChecked = true;
                popup.NotebookButton.Visibility = Visibility.Visible;
                popup.CountToken.Text = "LOOKED UP 03";
                popup.UseSize(new Size(PopupWindow.DefaultWidth, PopupWindow.DefaultHeight)); // a remembered size shows "DEFAULT SIZE"
                Flush();
                Assert.Equal(Visibility.Visible, popup.ResetSizeButton.Visibility);
                Snapshot(popup, $"popup-{name}.png");
                popup.Close();

                var prompt = OffScreen(new PromptWindow());
                prompt.Input.Text = "Architecture";
                prompt.ActionButton.Content = "Create";
                Flush();
                Snapshot(prompt, $"prompt-{name}.png");
                prompt.Close();

                // The tray menu is a stock ContextMenu with these items (TrayIcon itself would add a real tray icon).
                var menu = new System.Windows.Controls.ContextMenu();
                menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Look up a word…", InputGestureText = "Ctrl+Alt+F" });
                menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Notebook", InputGestureText = "Ctrl+Alt+N" });
                menu.Items.Add(new System.Windows.Controls.Separator());
                menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "How to use" });
                menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Settings…" });
                menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Quit Inset" });
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
                menu.HorizontalOffset = -12000;
                menu.IsOpen = true;
                Flush();
                if (Environment.GetEnvironmentVariable("LOOKUP_SNAPSHOT_DIR") is { Length: > 0 } menuDir)
                    SaveElementPng(menu, Path.Combine(menuDir, $"tray-menu-{name}.png"));
                menu.IsOpen = false;

                // The guide is a WebView page the window serves itself: it must load with no network.
                var guide = OffScreen(new GuideWindow { UserDataFolder = Path.Combine(dir, "guide-webview2") });
                Assert.True(Wait(guide.Shown), "the guide page did not load");
                Assert.Equal("\"Inset 使用說明\"", Wait(guide.Web.CoreWebView2.ExecuteScriptAsync("document.title")));
                if (Environment.GetEnvironmentVariable("LOOKUP_SNAPSHOT_DIR") is { Length: > 0 } guideDir)
                {
                    using var png = File.Create(Path.Combine(guideDir, $"guide-{name}.png"));
                    Wait(guide.Web.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, png)
                        .ContinueWith(_ => true));
                }
                guide.Close();
            }
            ThemeService.Apply(AppTheme.Light);
        }
        finally
        {
            // The guide's WebView2 browser can outlive its window for a moment and hold its profile open.
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    });

    /// <summary>Keeps the dispatcher running (WebView2 needs it) until the task finishes or times out.</summary>
    static T Wait<T>(Task<T> task, int seconds = 30)
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        var timeout = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        timeout.Tick += (_, _) => frame.Continue = false;
        task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
        timeout.Start();
        System.Windows.Threading.Dispatcher.PushFrame(frame);
        timeout.Stop();
        Assert.True(task.IsCompletedSuccessfully, $"timed out after {seconds}s");
        return task.Result;
    }

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

    /// <summary>For elements that live in a popup rather than a window, such as a context menu.</summary>
    static void SaveElementPng(FrameworkElement element, string path)
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

    static void Flush() =>
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

    /// <summary>Renders the window's client area as built: background, border, margins and all.</summary>
    static void SavePng(Window window, string path)
    {
        var client = (FrameworkElement)VisualTreeHelper.GetChild(window, 0); // the window template's root
        var dpi = VisualTreeHelper.GetDpi(window);
        var bitmap = new RenderTargetBitmap(
            (int)(client.ActualWidth * dpi.DpiScaleX), (int)(client.ActualHeight * dpi.DpiScaleY),
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(window);
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
