using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LookUp.Notebook;

/// <summary>
/// Saved words, user categories and lookup counts, kept in one JSON file.
/// Every change is written straight away; the file is small and there is a single instance of the app.
/// </summary>
sealed class NotebookStore
{
    /// <summary>Looking the same word up again within this window counts once.</summary>
    public static readonly TimeSpan RepeatLookupWindow = TimeSpan.FromMinutes(30);

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    readonly string _path;
    readonly Func<DateTime> _now;
    readonly Dictionary<string, LookupStat> _lookups = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<WordNote> Words { get; } = [];
    public ObservableCollection<string> Categories { get; } = [];

    /// <summary>Raised after any change, for views that show counts.</summary>
    public event Action? Changed;

    NotebookStore(string path, Func<DateTime>? now)
    {
        _path = path;
        _now = now ?? (() => DateTime.Now);
    }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LookUp", "notebook.json");

    /// <summary>
    /// Loads the notebook, or starts an empty one. An unreadable file is kept aside
    /// (notebook.unreadable-*.json) rather than overwritten.
    /// </summary>
    public static NotebookStore Load(string path, Func<DateTime>? now = null)
    {
        var store = new NotebookStore(path, now);
        if (File.Exists(path))
        {
            try
            {
                var data = JsonSerializer.Deserialize<NotebookFile>(File.ReadAllText(path), JsonOptions) ?? new();
                foreach (var stat in data.Lookups) store._lookups[stat.Word] = stat;
                foreach (var category in data.Categories) store.Categories.Add(category);
                foreach (var word in data.Words)
                {
                    store.Attach(word);
                    store.Words.Add(word);
                }
            }
            catch (JsonException)
            {
                File.Move(path, Path.ChangeExtension(path, $".unreadable-{store._now():yyyyMMdd-HHmmss}.json"));
                store = new NotebookStore(path, now);
            }
        }
        return store;
    }

    public WordNote? Find(string word) =>
        Words.FirstOrDefault(w => w.Word.Equals(word, StringComparison.OrdinalIgnoreCase));

    public LookupStat? GetLookups(string word) => _lookups.GetValueOrDefault(word);

    /// <summary>Adds the entry as an uncategorised new word, or returns it if it is already saved.</summary>
    public WordNote Add(EntrySummary entry)
    {
        if (Find(entry.Headword) is { } existing) return existing;
        var note = WordNote.From(entry, _now());
        Attach(note);
        Words.Insert(0, note);
        Save();
        return note;
    }

    public void Remove(WordNote note)
    {
        note.PropertyChanged -= OnWordChanged;
        Words.Remove(note);
        Save();
    }

    public LookupStat RecordLookup(string word)
    {
        var now = _now();
        if (!_lookups.TryGetValue(word, out var stat))
        {
            stat = new LookupStat { Word = word, First = now };
            _lookups[word] = stat;
        }
        else if (now - stat.Last < RepeatLookupWindow)
        {
            return stat;
        }

        stat.Count++;
        stat.Last = now;
        if (Find(word) is { } note)
        {
            note.Lookups = null; // same instance; force the count to refresh in bound views
            note.Lookups = stat;
        }
        Save();
        return stat;
    }

    /// <summary>Sets the word's category, creating the category if it is new. Blank means uncategorised.</summary>
    public void SetCategory(WordNote note, string category)
    {
        category = category.Trim();
        if (category.Length > 0 && !Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
            Categories.Add(category);
        else if (category.Length > 0)
            category = Categories.First(c => c.Equals(category, StringComparison.OrdinalIgnoreCase));

        note.Category = category; // saves through OnWordChanged
        Save();
    }

    /// <returns>false if the name is blank or already used.</returns>
    public bool AddCategory(string name)
    {
        name = name.Trim();
        if (name.Length == 0 || Categories.Contains(name, StringComparer.OrdinalIgnoreCase)) return false;
        Categories.Add(name);
        Save();
        return true;
    }

    /// <returns>false if the new name is blank or belongs to another category.</returns>
    public bool RenameCategory(string oldName, string newName)
    {
        newName = newName.Trim();
        var index = Categories.IndexOf(oldName);
        if (index < 0 || newName.Length == 0) return false;
        if (!newName.Equals(oldName, StringComparison.OrdinalIgnoreCase) &&
            Categories.Contains(newName, StringComparer.OrdinalIgnoreCase)) return false;

        Categories[index] = newName;
        foreach (var word in Words.Where(w => w.Category == oldName)) word.Category = newName;
        Save();
        return true;
    }

    /// <summary>Deletes the category; its words become uncategorised.</summary>
    public void DeleteCategory(string name)
    {
        if (!Categories.Remove(name)) return;
        foreach (var word in Words.Where(w => w.Category == name)) word.Category = "";
        Save();
    }

    void Attach(WordNote note)
    {
        note.Lookups = _lookups.GetValueOrDefault(note.Word);
        note.PropertyChanged += OnWordChanged;
    }

    void OnWordChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WordNote.Category) or nameof(WordNote.Status) or nameof(WordNote.Note))
            Save();
    }

    void Save()
    {
        var data = new NotebookFile
        {
            Categories = [.. Categories],
            Words = [.. Words],
            Lookups = [.. _lookups.Values.OrderBy(s => s.Word, StringComparer.OrdinalIgnoreCase)],
        };
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, JsonOptions));
        File.Move(temp, _path, overwrite: true);
        Changed?.Invoke();
    }

    sealed class NotebookFile
    {
        public int Version { get; set; } = 1;
        public List<string> Categories { get; set; } = [];
        public List<WordNote> Words { get; set; } = [];
        public List<LookupStat> Lookups { get; set; } = [];
    }
}
