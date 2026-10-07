using System.IO;
using LookUp.Notebook;
using LookUp.Settings;

namespace LookUp.Tests;

public sealed class NotebookBackupsTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "LookUpTests", Guid.NewGuid().ToString("N"));
    readonly string _path;
    readonly string _backupFolder;
    DateTime _now = new(2026, 10, 7, 9, 0, 0);

    public NotebookBackupsTests()
    {
        _path = Path.Combine(_dir, "Roaming", "notebook.json");
        _backupFolder = Path.Combine(_dir, "Local", "Backups");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    NotebookStore Open() => NotebookStore.Load(_path, () => _now);
    NotebookBackups Backups() => new(_backupFolder, () => _now);

    static EntrySummary Entry(string word) =>
        new(word, "noun", "ipa", "中文", "a definition", "An example.", "例句。", "");

    string[] BackupNames() =>
        Directory.Exists(_backupFolder) ? [.. Directory.GetFiles(_backupFolder).Select(f => Path.GetFileName(f)).Order()] : [];

    [Fact]
    public void One_copy_a_day_holds_the_notebook_as_it_was_first_backed_up()
    {
        var store = Open();
        store.Add(Entry("a"));
        Backups().Take(_path);
        store.Add(Entry("b"));
        Backups().Take(_path);

        Assert.Equal(["notebook-2026-10-07.json"], BackupNames());
        Assert.Equal(1, Backups().Latest()?.Words);

        _now = _now.AddDays(1);
        Backups().Take(_path);
        Assert.Equal(new DateTime(2026, 10, 8), Backups().Latest()?.Date);
        Assert.Equal(2, Backups().Latest()?.Words);
    }

    [Fact]
    public void An_empty_or_missing_notebook_is_never_backed_up()
    {
        Backups().Take(_path);
        var store = Open();
        store.RecordLookup("a");
        Backups().Take(_path);

        Assert.Empty(BackupNames());
        Assert.Null(Backups().Latest());
    }

    [Fact]
    public void Only_the_newest_copies_are_kept()
    {
        Open().Add(Entry("a"));
        for (var day = 0; day < NotebookBackups.Keep + 3; day++)
        {
            Backups().Take(_path);
            _now = _now.AddDays(1);
        }

        var names = BackupNames();
        Assert.Equal(NotebookBackups.Keep, names.Length);
        Assert.Equal("notebook-2026-10-10.json", names[0]);
    }

    [Fact]
    public void Latest_skips_copies_that_cannot_be_read()
    {
        Open().Add(Entry("a"));
        Backups().Take(_path);
        File.WriteAllText(Path.Combine(_backupFolder, "notebook-2026-10-08.json"), "{ not json");
        File.WriteAllText(Path.Combine(_backupFolder, "notes.json"), "{}");

        Assert.Equal(new DateTime(2026, 10, 7), Backups().Latest()?.Date);
    }

    [Fact]
    public void A_vanished_notebook_is_offered_back_from_its_backup_and_the_current_file_kept()
    {
        Open().Add(Entry("a"));
        Backups().Take(_path);

        // The notebook file is replaced by a fresh one with only a lookup in it.
        File.Delete(_path);
        _now = _now.AddDays(1);
        var fresh = Open();
        fresh.RecordLookup("later");

        var problem = NotebookRecovery.Check(Open(), _path, expectedWords: 1, Backups());
        Assert.NotNull(problem);
        Assert.NotNull(problem.Backup);
        Assert.Contains("2026-10-07", problem.Message);

        var restored = NotebookRecovery.Restore(_path, problem.Backup, () => _now);
        Assert.Equal("a", Assert.Single(restored.Words).Word);
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(_path)!, "notebook.replaced-*.json"));
    }

    [Fact]
    public void Nothing_is_reported_while_the_notebook_has_what_it_should()
    {
        Assert.Null(NotebookRecovery.Check(Open(), _path, expectedWords: 0, Backups()));

        Open().Add(Entry("a"));
        Assert.Null(NotebookRecovery.Check(Open(), _path, expectedWords: 5, Backups()));
    }

    [Fact]
    public void A_vanished_notebook_without_a_backup_is_still_reported()
    {
        var problem = NotebookRecovery.Check(Open(), _path, expectedWords: 3, Backups());

        Assert.NotNull(problem);
        Assert.Null(problem.Backup);
        Assert.Contains("3 saved word(s)", problem.Message);
    }

    [Fact]
    public void The_word_count_is_kept_in_the_registry()
    {
        var key = $@"Software\Inset.Tests\{Guid.NewGuid():N}";
        try
        {
            Assert.Equal(0, new NotebookWordCount(key).Value);
            new NotebookWordCount(key).Value = 12;
            Assert.Equal(12, new NotebookWordCount(key).Value);
        }
        finally
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\Inset.Tests", throwOnMissingSubKey: false);
        }
    }
}
