using System.Text;
using System.Text.RegularExpressions;

namespace LookUp.Lookup;

enum QueryProblem { None, Empty, NotEnglish, TooLong }

sealed record NormalizedQuery(string Text, int WordCount, QueryProblem Problem)
{
    public bool IsPhrase => WordCount > 1;
}

/// <summary>Turns a raw selection such as “Resilience,” or "resil-\nience" into a dictionary query.</summary>
static partial class QueryNormalizer
{
    public const int MaxWords = 6;

    public static NormalizedQuery Normalize(string? raw)
    {
        var text = (raw ?? "").Normalize(NormalizationForm.FormKC); // also folds full-width letters and ligatures

        text = text
            .Replace('’', '\'').Replace('‘', '\'').Replace('ʼ', '\'') // curly apostrophes
            .Replace('‐', '-').Replace('‑', '-')                            // Unicode hyphens
            .Replace("­", "");                                                  // soft hyphen

        text = LineBreakHyphen().Replace(text, "$1$2");   // "resil-\nience" → "resilience" (PDF line wraps)
        text = Whitespace().Replace(text, " ");
        text = EdgePunctuation().Replace(text, "");       // quotes, commas, brackets around the selection

        if (text.Length == 0) return new("", 0, QueryProblem.Empty);
        if (!AsciiLetter().IsMatch(text)) return new(text, 0, QueryProblem.NotEnglish);

        var words = text.Split(' ').Length;
        return new(text, words, words > MaxWords ? QueryProblem.TooLong : QueryProblem.None);
    }

    [GeneratedRegex(@"([A-Za-z])-\s*\r?\n\s*([a-z])")]
    private static partial Regex LineBreakHyphen();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    // Anything that is not a letter or digit at either end.
    [GeneratedRegex(@"^[^\p{L}\p{N}]+|[^\p{L}\p{N}]+$")]
    private static partial Regex EdgePunctuation();

    [GeneratedRegex(@"[A-Za-z]")]
    private static partial Regex AsciiLetter();
}
