using LookUp.Lookup;

namespace LookUp.Notebook;

/// <summary>
/// Files the AI's short conclusion into the word's note when the user copies it from the reply.
/// Only words asked about in the last <see cref="Window"/> are expected; a word not yet in the
/// notebook is added from the entry it was asked from.
/// </summary>
sealed class AiConclusions
{
    public static readonly TimeSpan Window = TimeSpan.FromHours(2);

    readonly NotebookStore _store;
    readonly Func<DateTime> _now;
    readonly Dictionary<string, (EntrySummary? Entry, DateTime Asked)> _pending = new(StringComparer.OrdinalIgnoreCase);

    public AiConclusions(NotebookStore store, Func<DateTime>? now = null)
    {
        _store = store;
        _now = now ?? (() => DateTime.Now);
    }

    /// <summary>A question about the word was just sent. The entry is null when the word is already saved.</summary>
    public void Expect(string word, EntrySummary? entry) => _pending[word] = (entry, _now());

    public bool IsExpecting
    {
        get
        {
            foreach (var word in _pending.Where(p => _now() - p.Value.Asked > Window).Select(p => p.Key).ToList())
                _pending.Remove(word);
            return _pending.Count > 0;
        }
    }

    /// <returns>The note the conclusion went into, or null if the text holds none for an expected word.</returns>
    public WordNote? Accept(string text)
    {
        if (!IsExpecting || AskAi.FindConclusion(text) is not { } conclusion) return null;

        // The AI may shorten a long headword ("account for"); with one question open, it can only be that one.
        var word = _pending.Keys.FirstOrDefault(w => w.Equals(conclusion.Word, StringComparison.OrdinalIgnoreCase))
                   ?? (_pending.Count == 1 ? _pending.Keys.First() : null);
        if (word == null) return null;

        var entry = _pending[word].Entry;
        var note = _store.Find(word) ?? (entry != null ? _store.Add(entry) : null);
        if (note == null) return null; // removed from the notebook since

        // Kept expecting: a follow-up in the same chat can add another conclusion. Copying one twice changes nothing.
        if (!note.Note.Contains(conclusion.Text))
            note.Note = note.Note.Length == 0 ? conclusion.Text : note.Note.TrimEnd() + "\n" + conclusion.Text;
        return note;
    }
}
