using System.IO;
using System.Text;

namespace LookUp.Notebook;

/// <summary>Writes words as CSV that Excel opens correctly (UTF-8 with BOM) and Anki can import.</summary>
static class CsvExport
{
    static readonly string[] Header =
    [
        "Word", "Part of speech", "IPA", "Chinese", "Definition", "Example", "Example (Chinese)",
        "Category", "Status", "Note", "Times looked up", "Added", "Last looked up", "Cambridge URL",
    ];

    public static void Write(string path, IEnumerable<WordNote> words)
    {
        using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Write(writer, words);
    }

    public static void Write(TextWriter writer, IEnumerable<WordNote> words)
    {
        WriteRow(writer, Header);
        foreach (var w in words)
        {
            WriteRow(writer,
            [
                w.Word, w.PartOfSpeech, w.Ipa, w.Chinese, w.Definition, w.Example, w.ExampleChinese,
                w.Category, w.Status.ToString(), w.Note, w.LookupCount.ToString(),
                w.AddedAt.ToString("yyyy-MM-dd"), w.Lookups?.Last.ToString("yyyy-MM-dd") ?? "", w.SourceUrl,
            ]);
        }
    }

    static void WriteRow(TextWriter writer, IEnumerable<string> fields) =>
        writer.Write(string.Join(",", fields.Select(Escape)) + "\r\n");

    static string Escape(string field) =>
        field.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + field.Replace("\"", "\"\"") + "\"" : field;
}
