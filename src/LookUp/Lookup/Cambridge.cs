using System.IO;
using System.Reflection;
using System.Text.Json;

namespace LookUp.Lookup;

/// <summary>URLs and the reader-mode script for dictionary.cambridge.org.</summary>
static class Cambridge
{
    public const string Host = "dictionary.cambridge.org";
    const string Dataset = "english-chinese-traditional";

    /// <summary>Cambridge's own search redirects inflections and phrases to the right entry, or to a spellcheck page.</summary>
    public static Uri SearchUrl(string query) =>
        new($"https://{Host}/search/direct/?datasetsearch={Dataset}&q={Uri.EscapeDataString(query)}");

    public static Uri WebSearchUrl(string query) =>
        new($"https://www.google.com/search?q={Uri.EscapeDataString(query + " meaning")}");

    /// <summary>An entry from the English-only dictionary rather than English–Chinese.</summary>
    public static bool IsEnglishOnly(string entryUrl) =>
        Uri.TryCreate(entryUrl, UriKind.Absolute, out var uri) &&
        uri.AbsolutePath.StartsWith("/dictionary/english/", StringComparison.OrdinalIgnoreCase);

    public static bool IsDictionaryHost(Uri uri) =>
        uri.Host.Equals(Host, StringComparison.OrdinalIgnoreCase);

    /// <summary>Best-effort query text for an entry URL such as /dictionary/english-chinese-traditional/account-for.</summary>
    public static string QueryFromEntryUrl(Uri uri) =>
        Uri.UnescapeDataString(uri.Segments[^1].TrimEnd('/')).Replace('-', ' ');

    /// <summary>reader.js with reader.css inlined; runs in every top-level document.</summary>
    public static string ReaderScript()
    {
        var css = ReadResource("reader.css");
        return ReadResource("reader.js").Replace("__LU_CSS__", JsonSerializer.Serialize(css));
    }

    static string ReadResource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
                           ?? throw new InvalidOperationException($"Missing resource {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
