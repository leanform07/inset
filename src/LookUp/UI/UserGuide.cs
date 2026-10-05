using System.Diagnostics;
using System.IO;
using System.Reflection;
using LookUp.Settings;

namespace LookUp.UI;

/// <summary>
/// The how-to guide (Web/guide.html, Traditional Chinese). The same page ships in the release zip,
/// so it opens in the default browser rather than in a window of the app's own.
/// </summary>
static class UserGuide
{
    public static string Html()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("guide.html")
            ?? throw new InvalidOperationException("guide.html is not embedded");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Writes the guide next to the app's other local data (rewritten each time, so it matches this version) and opens it.</summary>
    public static void Open()
    {
        Directory.CreateDirectory(AppFolders.Local);
        var path = Path.Combine(AppFolders.Local, "guide.html");
        File.WriteAllText(path, Html());
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
