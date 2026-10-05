using System.IO;
using System.Reflection;
using System.Windows;

namespace LookUp.UI;

/// <summary>
/// The how-to guide (Web/guide.html, Traditional Chinese), shown in <see cref="GuideWindow"/>.
/// The same page ships in the release zip as a file to open in a browser.
/// </summary>
static class UserGuide
{
    static GuideWindow? _window;

    public static string Html()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("guide.html")
            ?? throw new InvalidOperationException("guide.html is not embedded");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Opens the guide, or brings the open one to the front.</summary>
    public static void Open()
    {
        if (_window == null)
        {
            _window = new GuideWindow();
            _window.Closed += (_, _) => _window = null;
        }
        _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        _window.Activate();
    }
}
