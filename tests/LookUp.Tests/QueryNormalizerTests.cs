using LookUp.Lookup;

namespace LookUp.Tests;

public sealed class QueryNormalizerTests
{
    [Theory]
    [InlineData("resilience", "resilience")]
    [InlineData("  resilience\r\n", "resilience")]
    [InlineData("“resilience,”", "resilience")]
    [InlineData("(resilience).", "resilience")]
    [InlineData("account   for", "account for")]
    [InlineData("account\nfor", "account for")]
    [InlineData("don’t", "don't")]
    [InlineData("resil-\nience", "resilience")]
    [InlineData("resil-\r\n  ience", "resilience")]
    [InlineData("well-known", "well-known")]
    [InlineData("ｒｅｓｉｌｉｅｎｃｅ", "resilience")]          // full-width letters
    [InlineData("re­silience", "resilience")]          // soft hyphen
    [InlineData("students'", "students")]
    public void Cleans_up_a_selection(string raw, string expected)
    {
        var query = QueryNormalizer.Normalize(raw);
        Assert.Equal(expected, query.Text);
        Assert.Equal(QueryProblem.None, query.Problem);
    }

    [Theory]
    [InlineData("resilience", 1)]
    [InlineData("account for", 2)]
    [InlineData("be subject to", 3)]
    [InlineData("one two three four five six", 6)]
    public void Counts_words(string raw, int words)
    {
        var query = QueryNormalizer.Normalize(raw);
        Assert.Equal(words, query.WordCount);
        Assert.Equal(words > 1, query.IsPhrase);
    }

    [Fact]
    public void More_than_six_words_is_too_long()
    {
        var query = QueryNormalizer.Normalize("The city needs greater resilience against floods.");
        Assert.Equal(QueryProblem.TooLong, query.Problem);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("“ ”")]
    public void Nothing_to_look_up_is_empty(string? raw) =>
        Assert.Equal(QueryProblem.Empty, QueryNormalizer.Normalize(raw).Problem);

    [Theory]
    [InlineData("復原力")]
    [InlineData("2026")]
    public void Text_without_English_letters_is_rejected(string raw) =>
        Assert.Equal(QueryProblem.NotEnglish, QueryNormalizer.Normalize(raw).Problem);
}
