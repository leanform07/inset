using System.IO;
using LookUp.Notebook;

namespace LookUp.Tests;

public sealed class NotebookStoreTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "LookUpTests", Guid.NewGuid().ToString("N"));
    readonly string _path;
    DateTime _now = new(2026, 10, 5, 9, 0, 0);

    public NotebookStoreTests() => _path = Path.Combine(_dir, "notebook.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    NotebookStore Open() => NotebookStore.Load(_path, () => _now);

    static EntrySummary Entry(string word, string chinese = "中文") =>
        new(word, "noun", "ipa", chinese, "a definition", "An example.", "例句。",
            $"https://dictionary.cambridge.org/dictionary/english-chinese-traditional/{word}");

    [Fact]
    public void Add_saves_an_uncategorised_new_word_and_finds_it_ignoring_case()
    {
        var store = Open();

        var note = store.Add(Entry("resilience"));

        Assert.Same(note, store.Find("Resilience"));
        Assert.Equal("", note.Category);
        Assert.Equal(Familiarity.New, note.Status);
        Assert.Equal(_now, note.AddedAt);
    }

    [Fact]
    public void Adding_a_saved_word_again_returns_the_existing_note()
    {
        var store = Open();
        var first = store.Add(Entry("charge"));
        first.Note = "mine";

        var second = store.Add(Entry("charge", chinese: "different"));

        Assert.Same(first, second);
        Assert.Single(store.Words);
        Assert.Equal("mine", second.Note);
    }

    [Fact]
    public void Repeated_lookups_within_30_minutes_count_once()
    {
        var store = Open();

        store.RecordLookup("charge");
        _now = _now.AddMinutes(10);
        store.RecordLookup("charge");
        _now = _now.AddMinutes(31);
        var stat = store.RecordLookup("charge");

        Assert.Equal(2, stat.Count);
        Assert.Equal(new DateTime(2026, 10, 5, 9, 0, 0), stat.First);
        Assert.Equal(_now, stat.Last);
    }

    [Fact]
    public void Saved_words_show_lookups_made_before_and_after_saving()
    {
        var store = Open();
        store.RecordLookup("resilience");
        var note = store.Add(Entry("resilience"));
        Assert.Equal(1, note.LookupCount);

        _now = _now.AddHours(1);
        store.RecordLookup("resilience");

        Assert.Equal(2, note.LookupCount);
    }

    [Fact]
    public void Everything_survives_a_reload()
    {
        var store = Open();
        var note = store.Add(Entry("resilience"));
        store.SetCategory(note, "Architecture");
        note.Status = Familiarity.Learning;
        note.Note = "seen in a paper";
        store.AddCategory("Daily life");
        store.RecordLookup("resilience");
        store.RecordLookup("unsaved word");

        var reloaded = Open();

        var loaded = Assert.Single(reloaded.Words);
        Assert.Equal("Architecture", loaded.Category);
        Assert.Equal(Familiarity.Learning, loaded.Status);
        Assert.Equal("seen in a paper", loaded.Note);
        Assert.Equal("中文", loaded.Chinese);
        Assert.Equal(1, loaded.LookupCount);
        Assert.Equal(["Architecture", "Daily life"], reloaded.Categories);
        Assert.Equal(1, reloaded.GetLookups("unsaved word")?.Count);
    }

    [Fact]
    public void Edits_to_a_word_are_saved_without_an_explicit_call()
    {
        var store = Open();
        var note = store.Add(Entry("charge"));

        note.Note = "edited later";

        Assert.Equal("edited later", Open().Find("charge")?.Note);
    }

    [Fact]
    public void SetCategory_creates_new_categories_and_reuses_existing_spelling()
    {
        var store = Open();
        var a = store.Add(Entry("a"));
        var b = store.Add(Entry("b"));

        store.SetCategory(a, "  Architecture ");
        store.SetCategory(b, "architecture");

        Assert.Equal(["Architecture"], store.Categories);
        Assert.Equal("Architecture", b.Category);
    }

    [Fact]
    public void Blank_category_means_uncategorised()
    {
        var store = Open();
        var note = store.Add(Entry("a"));
        store.SetCategory(note, "Life");

        store.SetCategory(note, "  ");

        Assert.Equal("", note.Category);
    }

    [Fact]
    public void Renaming_a_category_moves_its_words_and_refuses_duplicates()
    {
        var store = Open();
        var note = store.Add(Entry("a"));
        store.SetCategory(note, "Life");
        store.AddCategory("Work");

        Assert.False(store.RenameCategory("Life", "work"));
        Assert.True(store.RenameCategory("Life", "Daily life"));

        Assert.Equal("Daily life", note.Category);
        Assert.Equal(["Daily life", "Work"], store.Categories);
    }

    [Fact]
    public void Deleting_a_category_keeps_its_words_as_uncategorised()
    {
        var store = Open();
        var note = store.Add(Entry("a"));
        store.SetCategory(note, "Life");

        store.DeleteCategory("Life");

        Assert.Empty(store.Categories);
        Assert.Equal("", note.Category);
        Assert.Single(store.Words);
    }

    [Fact]
    public void AddCategory_refuses_blank_and_duplicate_names()
    {
        var store = Open();
        Assert.True(store.AddCategory("Life"));
        Assert.False(store.AddCategory("life"));
        Assert.False(store.AddCategory(" "));
        Assert.Equal(["Life"], store.Categories);
    }

    [Fact]
    public void Removing_a_word_keeps_its_lookup_count()
    {
        var store = Open();
        store.RecordLookup("a");
        store.Remove(store.Add(Entry("a")));

        var reloaded = Open();
        Assert.Empty(reloaded.Words);
        Assert.Equal(1, reloaded.GetLookups("a")?.Count);
    }

    [Fact]
    public void An_unreadable_file_is_kept_aside_and_an_empty_notebook_starts()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_path, "{ not json");

        var store = Open();

        Assert.Empty(store.Words);
        Assert.False(File.Exists(_path));
        Assert.Equal(Directory.GetFiles(_dir, "notebook.unreadable-*.json").Single(), store.SetAsidePath);
    }

    [Fact]
    public void A_readable_or_missing_file_sets_nothing_aside()
    {
        Assert.Null(Open().SetAsidePath);
        Open().Add(Entry("a"));
        Assert.Null(Open().SetAsidePath);
    }

    [Fact]
    public void CountWords_reads_without_changing_anything()
    {
        Assert.Null(NotebookStore.CountWords(_path));

        var store = Open();
        store.Add(Entry("a"));
        store.Add(Entry("b"));
        Assert.Equal(2, NotebookStore.CountWords(_path));

        File.WriteAllText(_path, "{ not json");
        Assert.Null(NotebookStore.CountWords(_path));
        Assert.True(File.Exists(_path));
        Assert.Empty(Directory.GetFiles(_dir, "notebook.unreadable-*.json"));
    }
}
