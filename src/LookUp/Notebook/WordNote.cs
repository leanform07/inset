using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LookUp.Notebook;

enum Familiarity { New, Learning, Known }

/// <summary>The first sense of an entry, as reader.js reports it.</summary>
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
    public string PartOfSpeech { get; set; } = "";
    public string Ipa { get; set; } = "";
    public string Chinese { get; set; } = "";
    public string Definition { get; set; } = "";
    public string Example { get; set; } = "";
    public string ExampleChinese { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public DateTime AddedAt { get; set; }

    string _category = "";
    /// <summary>Empty means uncategorised.</summary>
    public string Category { get => _category; set => Set(ref _category, value); }

    Familiarity _status;
    public Familiarity Status { get => _status; set => Set(ref _status, value); }

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

    void Set<T>(ref T field, T value, [CallerMemberName] string name = "", string? notifyAlso = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (notifyAlso != null) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(notifyAlso));
    }
}
