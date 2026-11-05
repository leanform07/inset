using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LookUp.Notebook;

enum Familiarity { New, Learning, Known }

/// <summary>One sense of an entry (the first, unless the user picked another), as reader.js reports it.</summary>
sealed record EntrySummary(
    string Headword, string PartOfSpeech, string Ipa, string Chinese,
    string Definition, string Example, string ExampleChinese, string SourceUrl);

/// <summary>How often a word has been looked up, whether or not it is in the notebook.</summary>
sealed class LookupStat
{
    public string Word { get; set; } = "";
    public int Count { get; set; }
    public DateTime First { get; set; }
    public DateTime Last { get; set; }
}

/// <summary>A saved word. Category, status and note are edited by the user; the rest is copied from the entry.</summary>
sealed class WordNote : INotifyPropertyChanged
{
    public string Word { get; set; } = "";
    public string Ipa { get; set; } = "";
    public string SourceUrl { get; set; } = "";

    // The sense kept for the word; the user can switch to another sense of the entry.
    string _partOfSpeech = "", _chinese = "", _definition = "", _example = "", _exampleChinese = "";
    public string PartOfSpeech { get => _partOfSpeech; set => Set(ref _partOfSpeech, value); }
    public string Chinese { get => _chinese; set => Set(ref _chinese, value); }
    public string Definition { get => _definition; set => Set(ref _definition, value); }
    public string Example { get => _example; set => Set(ref _example, value); }
    public string ExampleChinese { get => _exampleChinese; set => Set(ref _exampleChinese, value); }
    public DateTime AddedAt { get; set; }

    string _category = "";
    /// <summary>Empty means uncategorised.</summary>
    public string Category { get => _category; set => Set(ref _category, value); }

    Familiarity _status;
    public Familiarity Status { get => _status; set => Set(ref _status, value, notifyAlso: nameof(StatusLabel)); }

    /// <summary>The status as the notebook's mono column shows it.</summary>
    [JsonIgnore]
    public string StatusLabel => _status.ToString().ToUpperInvariant();

    string _note = "";
    public string Note { get => _note; set => Set(ref _note, value); }

    LookupStat? _lookups;
    /// <summary>Kept in sync by <see cref="NotebookStore"/>; stored separately.</summary>
    [JsonIgnore]
    public LookupStat? Lookups { get => _lookups; set => Set(ref _lookups, value, notifyAlso: nameof(LookupCount)); }

    [JsonIgnore]
    public int LookupCount => _lookups?.Count ?? 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public static WordNote From(EntrySummary entry, DateTime now) => new()
    {
        Word = entry.Headword,
        PartOfSpeech = entry.PartOfSpeech,
        Ipa = entry.Ipa,
        Chinese = entry.Chinese,
        Definition = entry.Definition,
        Example = entry.Example,
        ExampleChinese = entry.ExampleChinese,
        SourceUrl = entry.SourceUrl,
        AddedAt = now,
    };

    /// <summary>Keeps another sense of the same entry; category, status and note stay.</summary>
    public void UseSense(EntrySummary sense)
    {
        PartOfSpeech = sense.PartOfSpeech;
        Chinese = sense.Chinese;
        Definition = sense.Definition;
        Example = sense.Example;
        ExampleChinese = sense.ExampleChinese;
        if (sense.Ipa.Length > 0) Ipa = sense.Ipa;
        if (sense.SourceUrl.Length > 0) SourceUrl = sense.SourceUrl;
    }

    void Set<T>(ref T field, T value, [CallerMemberName] string name = "", string? notifyAlso = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (notifyAlso != null) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(notifyAlso));
    }
}
