using System.IO;
using LookUp.Settings;

namespace LookUp.Tests;

public sealed class AppFoldersTests : IDisposable
{
    readonly string _parent = Path.Combine(Path.GetTempPath(), "LookUpTests", Guid.NewGuid().ToString("N"));

    public AppFoldersTests() => Directory.CreateDirectory(_parent);

    public void Dispose() => Directory.Delete(_parent, recursive: true);

    [Fact]
    public void Data_under_the_former_name_moves_to_the_new_folder()
    {
        Directory.CreateDirectory(Path.Combine(_parent, "LookUp"));
        File.WriteAllText(Path.Combine(_parent, "LookUp", "notebook.json"), "{}");

        AppFolders.MoveFromFormerName(_parent);

        Assert.True(File.Exists(Path.Combine(_parent, "Inset", "notebook.json")));
        Assert.False(Directory.Exists(Path.Combine(_parent, "LookUp")));
        Assert.Equal(Path.Combine(_parent, "Inset"), AppFolders.Resolve(_parent));
    }

    [Fact]
    public void An_existing_new_folder_is_never_overwritten()
    {
        Directory.CreateDirectory(Path.Combine(_parent, "LookUp"));
        Directory.CreateDirectory(Path.Combine(_parent, "Inset"));
        File.WriteAllText(Path.Combine(_parent, "Inset", "notebook.json"), "new");

        AppFolders.MoveFromFormerName(_parent);

        Assert.Equal("new", File.ReadAllText(Path.Combine(_parent, "Inset", "notebook.json")));
        Assert.True(Directory.Exists(Path.Combine(_parent, "LookUp")));
    }

    [Fact]
    public void The_former_folder_is_used_until_it_can_be_moved()
    {
        Directory.CreateDirectory(Path.Combine(_parent, "LookUp"));
        Assert.Equal(Path.Combine(_parent, "LookUp"), AppFolders.Resolve(_parent));
    }

    [Fact]
    public void A_fresh_install_uses_the_new_folder()
    {
        AppFolders.MoveFromFormerName(_parent);
        Assert.Equal(Path.Combine(_parent, "Inset"), AppFolders.Resolve(_parent));
        Assert.False(Directory.Exists(Path.Combine(_parent, "Inset"))); // created on first save, not here
    }
}
