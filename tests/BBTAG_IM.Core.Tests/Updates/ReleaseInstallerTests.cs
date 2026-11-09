using System.IO.Compression;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Updates;

public sealed class ReleaseInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bbtag-release-" + Guid.NewGuid().ToString("N"));
    private readonly string _game;

    public ReleaseInstallerTests()
    {
        _game = Path.Combine(_root, "game");
        Directory.CreateDirectory(_game);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    public ReleaseInstaller CreateReleaseInstaller() => new();

    private string Zip(params (string Path, string Text)[] entries)
    {
        string path = Path.Combine(_root, "release.zip");

        using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create);

        foreach (var (entry, text) in entries)
        {
            using StreamWriter writer = new(archive.CreateEntry(entry).Open());
            writer.Write(text);
        }

        return path;
    }

    private string GameFile(string path) => Path.Combine(_game, path);

    [Fact]
    public void Install_Zip_PutsEveryFileInPlace()
    {
        // Arrange
        string zip = Zip(("BBTAG_Mod_Loader.exe", "loader"), ("mods/BBTAG-Improvement-Mod/ImprovementMod.dll", "mod"));

        // Act
        CreateReleaseInstaller().Install(zip, _game);

        // Assert
        Assert.Equal("loader", File.ReadAllText(GameFile("BBTAG_Mod_Loader.exe")));
        Assert.Equal("mod", File.ReadAllText(GameFile(@"mods\BBTAG-Improvement-Mod\ImprovementMod.dll")));
    }

    [Fact]
    public void Install_FileAlreadyThere_ReplacesItAndLeavesNoCopyBehind()
    {
        // Arrange
        File.WriteAllText(GameFile("dinput8.dll"), "old");
        string zip = Zip(("dinput8.dll", "new"));

        // Act
        CreateReleaseInstaller().Install(zip, _game);

        // Assert
        Assert.Equal("new", File.ReadAllText(GameFile("dinput8.dll")));
        Assert.Equal([GameFile("dinput8.dll")], Directory.GetFiles(_game));
    }

    [Fact]
    public void Install_EntryOutsideTheGameFolder_Throws()
    {
        // Arrange
        string zip = Zip(("../evil.dll", "x"));

        // Act
        Exception? exception = Record.Exception(() => CreateReleaseInstaller().Install(zip, _game));

        // Assert
        Assert.IsType<ModArchiveException>(exception);
        Assert.False(File.Exists(Path.Combine(_root, "evil.dll")));
    }

    [Fact]
    public void Install_FileHeldOpen_IsReplacedAndTheOldOneCleanedLater()
    {
        // Arrange
        File.WriteAllText(GameFile("BBTAG_IM_Updater.exe"), "old");
        string zip = Zip(("BBTAG_IM_Updater.exe", "new"));
        ReleaseInstaller installer = CreateReleaseInstaller();

        using (new FileStream(GameFile("BBTAG_IM_Updater.exe"), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
        {
            // Act
            installer.Install(zip, _game);
        }

        installer.RemoveLeftovers(_game);

        // Assert
        Assert.Equal("new", File.ReadAllText(GameFile("BBTAG_IM_Updater.exe")));
        Assert.Equal([GameFile("BBTAG_IM_Updater.exe")], Directory.GetFiles(_game));
    }

    [Fact]
    public void RemoveLeftovers_OldCopiesInTheModFolder_DeletesThemOnly()
    {
        // Arrange
        string modFolder = GameFile(@"mods\BBTAG-Improvement-Mod");
        Directory.CreateDirectory(modFolder);
        File.WriteAllText(Path.Combine(modFolder, "ImprovementMod.dll" + ReleaseInstaller.LeftoverSuffix), "old");
        File.WriteAllText(Path.Combine(modFolder, "ImprovementMod.json"), "{}");

        // Act
        CreateReleaseInstaller().RemoveLeftovers(_game);

        // Assert
        Assert.Equal([Path.Combine(modFolder, "ImprovementMod.json")], Directory.GetFiles(modFolder));
    }
}
