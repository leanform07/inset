using System.IO;

namespace LookUp.Settings;

/// <summary>Where the app keeps its data: %AppData%\Inset (settings, notebook) and %LocalAppData%\Inset (WebView2 profile).</summary>
static class AppFolders
{
    const string Name = "Inset";
    const string FormerName = "LookUp"; // the placeholder name, until October 2026

    public static string Roaming => Resolve(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
    public static string Local => Resolve(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    /// <summary>
    /// Moves the folders a copy named LookUp left behind. Does nothing once the new folders exist;
    /// if a folder is still in use (an old copy running), the move is tried again next start.
    /// </summary>
    public static void MoveFromFormerName()
    {
        MoveFromFormerName(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        MoveFromFormerName(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    }

    internal static void MoveFromFormerName(string parent)
    {
        var (current, former) = Paths(parent);
        if (Directory.Exists(current) || !Directory.Exists(former)) return;
        try
        {
            Directory.Move(former, current);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>Until a move succeeds, keep using the former folder rather than start empty.</summary>
    internal static string Resolve(string parent)
    {
        var (current, former) = Paths(parent);
        return !Directory.Exists(current) && Directory.Exists(former) ? former : current;
    }

    static (string Current, string Former) Paths(string parent) =>
        (Path.Combine(parent, Name), Path.Combine(parent, FormerName));
}
