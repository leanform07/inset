using System.Text.RegularExpressions;

namespace LookUp.Lookup;

/// <summary>The AI chat a follow-up question opens in.</summary>
enum AiAssistant { ChatGpt, Claude }

/// <summary>
/// Follow-up questions about a word (how it differs from another, how it is used), opened in the
/// user's own AI chat in the browser. Inset holds no keys and calls no API; the question travels in
/// the chat's ?q= address, which neither service documents, so the panel also offers to copy it.
/// The reply ends with a short conclusion in a code block headed by <see cref="Marker"/>; copying
/// it brings it back to the word's note (see AiConclusions).
/// </summary>
static partial class AskAi
{
    /// <summary>The panel's shortcuts (Compare, Usage, Collocations, Memory) plus the user's own question.</summary>
    public enum Kind { Usage, Compare, Collocations, Memory, Question }

    /// <summary>Starts the line that heads a conclusion, followed by the word: "[Inset] affect".</summary>
    public const string Marker = "[Inset]";

    public sealed record Conclusion(string Word, string Text);

    public static string Name(AiAssistant assistant) => assistant == AiAssistant.Claude ? "Claude" : "ChatGPT";

    /// <summary>
    /// What text typed without a shortcut asks for: nothing means more on usage; other English words
    /// or short phrases ("effect, influence") mean "how do they differ"; anything else is a question.
    /// </summary>
    public static Kind Classify(string input)
    {
        var text = input.Trim();
        if (text.Length == 0) return Kind.Usage;
        var others = OtherWords(text);
        return others.All(o => EnglishWords().IsMatch(o) && IsShortPhrase(o)) ? Kind.Compare : Kind.Question;
    }

    /// <summary>The words to compare with, as typed: "effect, influence" or "effect、influence".</summary>
    public static IReadOnlyList<string> OtherWords(string input) =>
        input.Split([',', '，', '、', ';', '；', '/'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    // "how is it used" is four plain words too; a question word first keeps it a question.
    static bool IsShortPhrase(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= 3 && !QuestionWords.Contains(words[0]);
    }

    static readonly HashSet<string> QuestionWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "what", "why", "when", "where", "which", "who", "is", "are", "can", "does", "do", "should",
    };

    [GeneratedRegex(@"^[A-Za-z][A-Za-z'’\- ]*$")]
    private static partial Regex EnglishWords();

    /// <summary>The question in Traditional Chinese, with the sense the user looked up so the AI explains that one.</summary>
    public static string Prompt(string word, string partOfSpeech, string chinese, string definition, Kind kind, string input = "")
    {
        var text = input.Trim();
        var sense = string.Join("：", new[] { partOfSpeech, chinese }.Where(s => s.Length > 0));
        var lines = new List<string>
        {
            sense.Length > 0 ? $"我在 Cambridge 英漢字典查了「{word}」（{sense}）。" : $"我在 Cambridge 字典查了「{word}」。",
        };
        if (definition.Length > 0) lines.Add($"英文解釋：{definition}");
        var others = string.Join("、", OtherWords(text).Select(o => $"「{o}」"));
        lines.Add(kind switch
        {
            Kind.Usage => "請更詳細說明這個字的用法：常見搭配、語氣、適合的場合，並附幾個例句。",
            Kind.Compare when OtherWords(text).Count > 1 =>
                $"它和{others}有什麼差別？請比較它們的意思、語氣和使用場合，各附例句。",
            Kind.Compare => $"它和{others}有什麼差別？請比較兩者的意思、語氣和使用場合，各附例句。",
            Kind.Collocations => "請列出這個字最常見的搭配詞（常一起用的動詞、形容詞、名詞、介係詞），各附一個例句，並說明哪些比較正式、哪些比較口語。",
            Kind.Memory => "請給我好記的記憶法：字根、字首、字源，或好聯想的記法，再附一個好記的例句。",
            _ => text,
        });
        // With Usage, Collocations or Memory chosen, the field is an optional note ("in architecture papers").
        if (kind is Kind.Usage or Kind.Collocations or Kind.Memory && text.Length > 0) lines.Add($"補充：{text}");
        lines.Add("請用繁體中文（台灣用語）回答，英文例句附中文翻譯。");
        // No line of the question may start with the marker, or copying the question would count as a conclusion.
        lines.Add($"最後，把重點整理成簡短的結論，放進一個程式碼區塊，區塊第一行寫「{Marker} {word}」，方便我複製回單字筆記本。");
        return string.Join("\n", lines);
    }

    /// <summary>
    /// The conclusion in copied text: the lines after a line starting with the marker, up to a code
    /// fence or the end. Works for the code block's own copy button and for a whole copied reply.
    /// </summary>
    public static Conclusion? FindConclusion(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!line.StartsWith(Marker, StringComparison.OrdinalIgnoreCase)) continue;
            var word = line[Marker.Length..].Trim().Trim('「', '」', '"', '“', '”', '`', '*', ':', '：').Trim();
            var body = string.Join("\n", lines.Skip(i + 1)
                .TakeWhile(l => !l.TrimStart().StartsWith("```"))
                .Select(l => l.Trim())
                .Where(l => l.Length > 0));
            if (word.Length > 0 && body.Length > 0) return new(word, body);
        }
        return null;
    }

    public static Uri ChatUrl(AiAssistant assistant, string prompt)
    {
        var q = Uri.EscapeDataString(prompt);
        return assistant == AiAssistant.Claude
            ? new($"https://claude.ai/new?q={q}")
            : new($"https://chatgpt.com/?q={q}");
    }
}
