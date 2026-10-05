using System.IO;
using System.Windows;
using LookUp.Settings;

namespace LookUp.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void Popup_size_survives_a_save_and_reset_removes_it()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lookup-settings-{Guid.NewGuid():N}.json");
        try
        {
            new AppSettings { PopupSize = new Size(520, 640) }.Save(path);
            Assert.Equal(new Size(520, 640), AppSettings.Load(path).PopupSize);

            new AppSettings { PopupSize = null }.Save(path);
            Assert.DoesNotContain("Popup", File.ReadAllText(path));
            Assert.Null(AppSettings.Load(path).PopupSize);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
