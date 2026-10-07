using System.Globalization;
using System.IO;

namespace LookUp.Notebook;

/// <summary>
/// One copy of the notebook per day, the last <see cref="Keep"/> days, kept in a folder apart from the notebook
/// so losing one folder does not lose both. An empty notebook is never backed up, so it can't push out good copies.
/// </summary>
sealed class NotebookBackups
{
    public const int Keep = 14;
    const string Prefix = "notebook-", DateFormat = "yyyy-MM-dd";

    readonly string _folder;
    readonly Func<DateTime> _now;

    public NotebookBackups(string folder, Func<DateTime>? now = null)
    {
        _folder = folder;
        _now = now ?? (() => DateTime.Now);
    }

    /// <summary>%LocalAppData%\Inset\Backups: not beside the notebook in %AppData%\Inset.</summary>
    public static string DefaultFolder => Path.Combine(Settings.AppFolders.Local, "Backups");

    public sealed record Backup(string Path, DateTime Date, int Words);

    /// <summary>Copies the notebook unless today's copy exists or it has no words. Never throws: a failed backup must not stop a save.</summary>
    public void Take(string notebookPath)
    {
        try
        {
            var target = Path.Combine(_folder, $"{Prefix}{_now().ToString(DateFormat, CultureInfo.InvariantCulture)}.json");
            if (File.Exists(target) || NotebookStore.CountWords(notebookPath) is not > 0) return;

            Directory.CreateDirectory(_folder);
            File.Copy(notebookPath, target);
            foreach (var old in All().Skip(Keep)) File.Delete(old.Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>The newest backup that can be read and has words, or null.</summary>
    public Backup? Latest()
    {
        foreach (var (path, date) in All())
            if (NotebookStore.CountWords(path) is int words && words > 0) return new Backup(path, date, words);
        return null;
    }

    /// <summary>Backups by date, newest first.</summary>
    IEnumerable<(string Path, DateTime Date)> All()
    {
        if (!Directory.Exists(_folder)) return [];
        var backups = new List<(string Path, DateTime Date)>();
        foreach (var path in Directory.EnumerateFiles(_folder, $"{Prefix}*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(path)[Prefix.Length..];
            if (DateTime.TryParseExact(name, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                backups.Add((path, date));
        }
        return backups.OrderByDescending(b => b.Date);
    }
}
