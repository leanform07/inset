using LookUp.Settings;
using LookUp.UI;

namespace LookUp.Tests;

public sealed class UserGuideTests
{
    [Fact]
    public void Guide_is_embedded_and_names_the_default_shortcuts()
    {
        var html = UserGuide.Html();
        Assert.Contains("<title>Inset 使用說明</title>", html);

        var settings = new AppSettings();
        foreach (var hotkey in new[] { settings.LookupSelection, settings.Search, settings.Notebook })
            Assert.Contains($"<kbd>{hotkey}</kbd>", html);
    }
}
