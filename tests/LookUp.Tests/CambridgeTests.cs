using LookUp.Lookup;

namespace LookUp.Tests;

public sealed class CambridgeTests
{
    [Fact]
    public void Search_urls_escape_phrases()
    {
        var url = Cambridge.SearchUrl("account for");
        Assert.Equal("https://dictionary.cambridge.org/search/direct/?datasetsearch=english-chinese-traditional&q=account%20for",
            url.AbsoluteUri);
    }

    [Theory]
    [InlineData("https://dictionary.cambridge.org/dictionary/english/deplatform", true)]
    [InlineData("https://dictionary.cambridge.org/dictionary/english-chinese-traditional/resilience", false)]
    [InlineData("not a url", false)]
    public void Recognises_English_only_entries(string url, bool englishOnly) =>
        Assert.Equal(englishOnly, Cambridge.IsEnglishOnly(url));

    [Theory]
    [InlineData("https://dictionary.cambridge.org/dictionary/english-chinese-traditional/account-for", "account for")]
    [InlineData("https://dictionary.cambridge.org/dictionary/english-chinese-traditional/ask", "ask")]
    public void Turns_entry_urls_back_into_queries(string url, string query) =>
        Assert.Equal(query, Cambridge.QueryFromEntryUrl(new Uri(url)));
}
