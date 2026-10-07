using System.IO;

namespace LookUp.Notebook;

/// <summary>Notices, at startup, a notebook that had words when last saved and now has none.</summary>
static class NotebookRecovery
{
    public sealed record Problem(string Message, NotebookBackups.Backup? Backup);

    /// <param name="expectedWords">How many words the notebook had when last saved.</param>
    /// <returns>null when the notebook looks as it was left.</returns>
    public static Problem? Check(NotebookStore loaded, string path, int expectedWords, NotebookBackups backups)
    {
        if (loaded.Words.Count > 0 || expectedWords == 0) return null;

        var what = loaded.SetAsidePath is { } aside
            ? $"Inset couldn't read your notebook. The file was kept as {Path.GetFileName(aside)}."
            : $"Your notebook had {expectedWords} saved word(s), but the notebook file in {Path.GetDirectoryName(path)} now has none.";
        var backup = backups.Latest();
        return new Problem(
            backup == null
                ? $"{what}\n\nNo backup was found, so Inset will start with an empty notebook."
                : $"{what}\n\nRestore the backup from {backup.Date:yyyy-MM-dd} ({backup.Words} word(s))?\nThe current notebook file is kept beside it.",
            backup);
    }

    /// <summary>Puts the backup in place of the notebook, keeping the current file as notebook.replaced-*.json.</summary>
    public static NotebookStore Restore(string path, NotebookBackups.Backup backup, Func<DateTime>? now = null)
    {
        now ??= () => DateTime.Now;
        if (File.Exists(path)) File.Move(path, Path.ChangeExtension(path, $".replaced-{now():yyyyMMdd-HHmmss}.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Copy(backup.Path, path);
        return NotebookStore.Load(path, now);
    }
}
