using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Updates;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Mods.Updates;

public sealed class ModSourceStoreTests : IDisposable
{
    private readonly string _mods = Path.Combine(Path.GetTempPath(), "bbtag-sources-" + Guid.NewGuid().ToString("N"));

    public ModSourceStoreTests() => Directory.CreateDirectory(_mods);

    public void Dispose() => Directory.Delete(_mods, recursive: true);

    public ModSourceStore CreateModSourceStore() => new(new PhysicalFileSystem());

    private string ModFolder(string name)
    {
        string folder = Path.Combine(_mods, name);
        Directory.CreateDirectory(folder);
        return folder;
    }

    [Fact]
    public void Load_FolderWithoutALink_ReturnsNull()
    {
        // Arrange
        string folder = ModFolder("Mod");

        // Act
        ModSource? source = CreateModSourceStore().Load(folder);

        // Assert
        Assert.Null(source);
    }

    [Fact]
    public void Load_SavedSource_ReadsItBack()
    {
        // Arrange
        string folder = ModFolder("Mod");
        ModSourceStore store = CreateModSourceStore();
        ModSource saved = new() { Repository = "https://github.com/owner/mod", Version = "abc" };
        saved.Files["data/a.pac"] = "s1";
        store.Save(folder, saved);

        // Act
        ModSource? source = store.Load(folder);

        // Assert
        Assert.NotNull(source);
        Assert.Equal("https://github.com/owner/mod", source.Repository);
        Assert.Equal("abc", source.Version);
        Assert.Equal("s1", source.Files["DATA/A.PAC"]);
    }

    [Fact]
    public void Load_BrokenFile_Throws()
    {
        // Arrange
        string folder = ModFolder("Mod");
        File.WriteAllText(Path.Combine(folder, ModSourceStore.FileName), "{ nope");

        // Act
        Exception? exception = Record.Exception(() => CreateModSourceStore().Load(folder));

        // Assert
        Assert.IsType<ModUpdateException>(exception);
    }

    [Fact]
    public void Link_NewRepository_ForgetsWhatTheOldOneSent()
    {
        // Arrange
        string folder = ModFolder("Mod");
        ModSourceStore store = CreateModSourceStore();
        ModSource saved = new() { Repository = "https://github.com/owner/old", Version = "abc" };
        saved.Files["data/a.pac"] = "s1";
        store.Save(folder, saved);

        // Act
        store.Link(folder, new GitHubRepository("owner", "new"));

        // Assert
        ModSource source = store.Load(folder)!;
        Assert.Equal("https://github.com/owner/new", source.Repository);
        Assert.Null(source.Version);
        Assert.Empty(source.Files);
    }

    [Fact]
    public void Link_SameRepository_KeepsWhatItSent()
    {
        // Arrange
        string folder = ModFolder("Mod");
        ModSourceStore store = CreateModSourceStore();
        store.Save(folder, new ModSource { Repository = "https://github.com/owner/mod", Version = "abc" });

        // Act
        store.Link(folder, new GitHubRepository("Owner", "Mod"));

        // Assert
        Assert.Equal("abc", store.Load(folder)!.Version);
    }

    [Fact]
    public void Unlink_LinkedFolder_RemovesTheLink()
    {
        // Arrange
        string folder = ModFolder("Mod");
        ModSourceStore store = CreateModSourceStore();
        store.Link(folder, new GitHubRepository("owner", "mod"));

        // Act
        store.Unlink(folder);

        // Assert
        Assert.Null(store.Load(folder));
    }

    [Fact]
    public void LinkedFolders_ModsFolder_ListsOnlyFoldersWithALink()
    {
        // Arrange
        ModSourceStore store = CreateModSourceStore();
        string linked = ModFolder("Linked");
        ModFolder("Plain");
        store.Link(linked, new GitHubRepository("owner", "mod"));

        // Act
        IReadOnlyList<string> folders = store.LinkedFolders(_mods);

        // Assert
        Assert.Equal([linked], folders);
    }

    [Fact]
    public void AddFolder_NewRepository_MakesALinkedFolderNamedAfterIt()
    {
        // Act
        string folder = CreateModSourceStore().AddFolder(_mods, new GitHubRepository("ZeroTwo-0", "BBTAG-OverHaul-Patches"));

        // Assert
        Assert.Equal(Path.Combine(_mods, "BBTAG-OverHaul-Patches"), folder);
        Assert.Equal("https://github.com/ZeroTwo-0/BBTAG-OverHaul-Patches", CreateModSourceStore().Load(folder)!.Repository);
    }

    [Fact]
    public void AddFolder_FolderAlreadyLinkedToTheSameRepository_ReturnsIt()
    {
        // Arrange
        ModSourceStore store = CreateModSourceStore();
        string existing = store.AddFolder(_mods, new GitHubRepository("owner", "mod"));

        // Act
        string folder = store.AddFolder(_mods, new GitHubRepository("owner", "mod"));

        // Assert
        Assert.Equal(existing, folder);
    }

    [Fact]
    public void AddFolder_FolderOfAnotherMod_Throws()
    {
        // Arrange
        ModFolder("mod");

        // Act
        Exception? exception = Record.Exception(() => CreateModSourceStore().AddFolder(_mods, new GitHubRepository("owner", "mod")));

        // Assert
        Assert.IsType<ModUpdateException>(exception);
    }

    [Fact]
    public void LinkedFolders_MissingModsFolder_IsEmpty()
    {
        // Act
        IReadOnlyList<string> folders = CreateModSourceStore().LinkedFolders(Path.Combine(_mods, "nothing"));

        // Assert
        Assert.Empty(folders);
    }
}
