using System.IO;
using LookUp.Lookup;
using LookUp.Notebook;

namespace LookUp.Tests;

public sealed class AiConclusionsTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "LookUpTests", Guid.NewGuid().ToString("N"));
    DateTime _now = new(2026, 10, 9, 14, 0, 0);
    readonly NotebookStore _store;
    readonly AiConclusions _conclusions;

    static readonly EntrySummary Affect = new("affect", "verb", "", "影響", "to have an influence on someone or something", "", "", "");

    public AiConclusionsTests()
    {
        _store = NotebookStore.Load(Path.Combine(_dir, "notebook.json"), () => _now);
        _conclusions = new AiConclusions(_store, () => _now);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    const string CodeBlock = "[Inset] affect\naffect 是動詞「影響」；effect 多半是名詞「效果」。\n記法：Affect = Action。";

    [Fact]
    public void Finds_the_conclusion_in_the_code_block_or_a_whole_copied_reply()
    {
        var block = AskAi.FindConclusion(CodeBlock);
        Assert.Equal(new AskAi.Conclusion("affect", "affect 是動詞「影響」；effect 多半是名詞「效果」。\n記法：Affect = Action。"), block);

        var reply = "兩者常被搞混……\r\n\r\n**結論**\r\n```\r\n" + CodeBlock.Replace("\n", "\r\n") + "\r\n```\r\n希望有幫助！";
        Assert.Equal(block, AskAi.FindConclusion(reply));

        Assert.Equal("affect", AskAi.FindConclusion("[Inset] `affect`\n結論")!.Word);
        Assert.Null(AskAi.FindConclusion("[Inset] affect\n```"));
        Assert.Null(AskAi.FindConclusion("affect 和 effect 的差別"));
    }

    [Fact]
    public void The_question_itself_is_not_taken_for_a_conclusion()
    {
        var question = AskAi.Prompt("affect", "verb", "影響", "", AskAi.Kind.Compare, "effect");
        Assert.Contains($"{AskAi.Marker} affect", question);
        Assert.Null(AskAi.FindConclusion(question));
    }

    [Fact]
    public void A_word_not_yet_saved_is_added_with_the_conclusion_as_its_note()
    {
        _conclusions.Expect("affect", Affect);
        var note = _conclusions.Accept(CodeBlock);
        Assert.NotNull(note);
        Assert.Same(note, _store.Find("affect"));
        Assert.Equal("影響", note.Chinese);
        Assert.StartsWith("affect 是動詞", note.Note);
    }

    [Fact]
    public void Conclusions_are_added_under_an_existing_note_once()
    {
        var note = _store.Add(Affect);
        note.Note = "Seen in a paper.";
        _conclusions.Expect("affect", null);

        _conclusions.Accept(CodeBlock);
        _conclusions.Accept(CodeBlock); // copied twice
        Assert.Equal("Seen in a paper.\naffect 是動詞「影響」；effect 多半是名詞「效果」。\n記法：Affect = Action。", note.Note);

        // A follow-up in the same chat adds its own conclusion.
        _conclusions.Accept("[Inset] affect\n正式寫作也用 affect。");
        Assert.EndsWith("\n正式寫作也用 affect。", note.Note);

        // Saved, so it survives a restart.
        Assert.Contains("正式寫作也用 affect。", NotebookStore.Load(Path.Combine(_dir, "notebook.json")).Find("affect")!.Note);
    }

    [Fact]
    public void Only_words_asked_about_recently_are_expected()
    {
        Assert.Null(_conclusions.Accept(CodeBlock)); // nothing asked
        Assert.False(_conclusions.IsExpecting);

        _conclusions.Expect("affect", Affect);
        Assert.True(_conclusions.IsExpecting);
        _now += AiConclusions.Window + TimeSpan.FromMinutes(1);
        Assert.False(_conclusions.IsExpecting);
        Assert.Null(_conclusions.Accept(CodeBlock));
        Assert.Null(_store.Find("affect"));
    }

    [Fact]
    public void A_shortened_headword_matches_when_only_one_question_is_open()
    {
        var entry = Affect with { Headword = "account (to someone) for something" };
        _conclusions.Expect(entry.Headword, entry);
        Assert.Equal(entry.Headword, _conclusions.Accept("[Inset] account for\n解釋原因。")?.Word);

        _conclusions.Expect("affect", Affect); // two open: no guessing
        Assert.Null(_conclusions.Accept("[Inset] effect\n效果。"));
    }
}
