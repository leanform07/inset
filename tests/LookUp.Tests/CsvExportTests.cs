using System.IO;
using System.Text;
using LookUp.Notebook;

namespace LookUp.Tests;

public sealed class CsvExportTests
{
    static string Export(params WordNote[] words)
    {
        var writer = new StringWriter();
        CsvExport.Write(writer, words);
        return writer.ToString();
    }

    [Fact]
    public void Writes_a_header_and_one_row_per_word()
    {
        var csv = Export(new WordNote
        {
            Word = "resilience", PartOfSpeech = "noun", Chinese = "復原力", Category = "Life",
            Status = Familiarity.Learning, AddedAt = new DateTime(2026, 10, 5),
        });

        var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("Word,Part of speech,IPA,Chinese,", lines[0]);
        Assert.StartsWith("resilience,noun,,復原力,", lines[1]);
        Assert.Contains(",Life,Learning,", lines[1]);
        Assert.Contains(",2026-10-05,", lines[1]);
    }

    [Fact]
    public void Quotes_fields_with_commas_quotes_or_line_breaks()
    {
        var csv = Export(new WordNote
        {
            Word = "charge",
            Definition = "to ask, especially",
            Note = "line one\nsaid \"hi\"",
        });

        Assert.Contains(",\"to ask, especially\",", csv);
        Assert.Contains(",\"line one\nsaid \"\"hi\"\"\",", csv);
    }

    [Fact]
    public void Files_start_with_a_UTF8_byte_order_mark_so_Excel_reads_Chinese()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lookup-{Guid.NewGuid():N}.csv");
        try
        {
            CsvExport.Write(path, [new WordNote { Word = "a", Chinese = "中文" }]);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal(Encoding.UTF8.Preamble.ToArray(), bytes[..3]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
