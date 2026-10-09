using System.IO;
using LookUp.Lookup;
using LookUp.Settings;

namespace LookUp.Tests;

public sealed class AskAiTests
{
    [Theory]
    [InlineData("", "Usage")]
    [InlineData("   ", "Usage")]
    [InlineData("effect", "Compare")]
    [InlineData(" well-being ", "Compare")]
    [InlineData("account for", "Compare")]
    [InlineData("can't", "Compare")]
    [InlineData("effect, influence", "Compare")]
    [InlineData("effect、impact，account for", "Compare")]
    [InlineData("effect, how is it used", "Question")]
    [InlineData("how is it used", "Question")]
    [InlineData("why not", "Question")]
    [InlineData("is it formal?", "Question")]
    [InlineData("這個字正式嗎", "Question")]
    [InlineData("the one used in contracts", "Question")]
    // The kind as text: AskAi is internal, and xunit theories must be public.
    public void Classifies_what_the_typed_text_asks_for(string input, string expected) =>
        Assert.Equal(expected, AskAi.Classify(input).ToString());

    [Fact]
    public void Prompt_names_the_sense_looked_up_and_the_other_word()
    {
        var prompt = AskAi.Prompt("affect", "verb", "影響", "to have an influence on someone or something", AskAi.Kind.Compare, " effect ");
        Assert.Contains("「affect」（verb：影響）", prompt);
        Assert.Contains("英文解釋：to have an influence on someone or something", prompt);
        Assert.Contains("它和「effect」有什麼差別？請比較兩者", prompt);
        Assert.Contains("繁體中文", prompt);
    }

    [Fact]
    public void Compare_takes_several_words()
    {
        var prompt = AskAi.Prompt("affect", "verb", "影響", "", AskAi.Kind.Compare, "effect, influence、impact");
        Assert.Contains("它和「effect」、「influence」、「impact」有什麼差別？請比較它們", prompt);
    }

    [Fact]
    public void Each_shortcut_asks_its_own_question_and_questions_pass_through()
    {
        string Ask(AskAi.Kind kind, string input = "") => AskAi.Prompt("affect", "verb", "影響", "", kind, input);
        Assert.Contains("用法", Ask(AskAi.Kind.Usage));
        Assert.Contains("搭配詞", Ask(AskAi.Kind.Collocations));
        Assert.Contains("記憶法", Ask(AskAi.Kind.Memory));
        Assert.DoesNotContain("英文解釋", Ask(AskAi.Kind.Usage));
        Assert.Contains("\n這個字正式嗎\n", Ask(AskAi.Kind.Question, "這個字正式嗎"));
        Assert.DoesNotContain("補充", Ask(AskAi.Kind.Collocations));
    }

    [Fact]
    public void Text_typed_with_a_shortcut_goes_along_as_a_note()
    {
        var prompt = AskAi.Prompt("affect", "verb", "影響", "", AskAi.Kind.Collocations, " 在建築論文裡 ");
        Assert.Contains("搭配詞", prompt);
        Assert.Contains("\n補充：在建築論文裡\n", prompt);
    }

    [Fact]
    public void Prompt_without_Chinese_leaves_out_the_empty_sense()
    {
        var prompt = AskAi.Prompt("zeitgeber", "", "", "", AskAi.Kind.Usage);
        Assert.StartsWith("我在 Cambridge 字典查了「zeitgeber」。", prompt);
    }

    [Fact]
    public void Chat_urls_carry_the_whole_question()
    {
        var prompt = "「affect」和「effect」？ a&b=c";
        var chatGpt = AskAi.ChatUrl(AiAssistant.ChatGpt, prompt);
        var claude = AskAi.ChatUrl(AiAssistant.Claude, prompt);
        Assert.Equal("chatgpt.com", chatGpt.Host);
        Assert.Equal("claude.ai", claude.Host);
        Assert.Equal("/new", claude.AbsolutePath);
        foreach (var url in new[] { chatGpt, claude })
            Assert.Equal(prompt, Uri.UnescapeDataString(url.Query["?q=".Length..]));
    }

    [Fact]
    public void Settings_default_to_ChatGPT_and_keep_the_choice()
    {
        var path = Path.Combine(Path.GetTempPath(), "LookUpTests", Guid.NewGuid().ToString("N"), "settings.json");
        try
        {
            Assert.Equal(AiAssistant.ChatGpt, new AppSettings().AiAssistant);
            new AppSettings { AiAssistant = AiAssistant.Claude }.Save(path);
            Assert.Contains("\"AiAssistant\": \"Claude\"", File.ReadAllText(path));
            Assert.Equal(AiAssistant.Claude, AppSettings.Load(path).AiAssistant);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
