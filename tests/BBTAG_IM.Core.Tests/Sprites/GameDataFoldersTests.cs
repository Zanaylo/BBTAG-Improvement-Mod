using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Sprites;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Sprites;

public class GameDataFoldersTests
{
    private const string GameFolder = @"D:\BBTAG";
    private const string ModsFolder = @"D:\BBTAG\mods";
    private const string ImprovementMod = @"D:\BBTAG\mods\BBTAG-Improvement-Mod";
    private const string Overhaul = @"D:\BBTAG\mods\BBTAG-OverHaul";
    private const string Costumes = @"D:\BBTAG\mods\Costumes";
    private const string LauncherSettingsPath = @"D:\BBTAG\BBTAG_IM.json";
    private const string SteamGameFolder = @"C:\Steam\BBTAG";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public GameDataFoldersTests()
    {
        _fileSystemMock.DirectoryExists(ModsFolder).Returns(true);
        _fileSystemMock.EnumerateDirectories(ModsFolder).Returns([ImprovementMod, Overhaul, Costumes]);
        ModWithData(Overhaul);
        ModWithData(Costumes);
        _fileSystemMock.DirectoryExists(ImprovementMod).Returns(true);
        _fileSystemMock.EnumerateFiles(ImprovementMod).Returns([ImprovementMod + @"\ImprovementMod.dll"]);
    }

    [Fact]
    public void Of_EnabledModsWithData_ListsTheirDataFoldersThenTheGameData()
    {
        // Arrange
        _fileSystemMock.FileExists(LauncherSettingsPath).Returns(false);

        // Act
        IReadOnlyList<string> folders = GameDataFolders.Of(ImprovementMod, GameFolder, _fileSystemMock);

        // Assert
        Assert.Equal([Overhaul + @"\data", Costumes + @"\data", GameFolder + @"\data"], folders);
    }

    [Fact]
    public void Of_ModTurnedOffInTheLauncher_LeavesItOut()
    {
        // Arrange
        _fileSystemMock.FileExists(LauncherSettingsPath).Returns(true);
        _fileSystemMock.ReadAllText(LauncherSettingsPath).Returns("""{ "DisabledMods": ["Costumes"] }""");

        // Act
        IReadOnlyList<string> folders = GameDataFolders.Of(ImprovementMod, GameFolder, _fileSystemMock);

        // Assert
        Assert.Equal([Overhaul + @"\data", GameFolder + @"\data"], folders);
    }

    [Fact]
    public void Of_GameOutsideTheLoaderFolder_EndsWithThatGamesData()
    {
        // Arrange
        _fileSystemMock.FileExists(LauncherSettingsPath).Returns(false);

        // Act
        IReadOnlyList<string> folders = GameDataFolders.Of(ImprovementMod, SteamGameFolder, _fileSystemMock);

        // Assert
        Assert.Equal([Overhaul + @"\data", Costumes + @"\data", SteamGameFolder + @"\data"], folders);
    }

    [Fact]
    public void GameOnly_EnabledModsAround_ListsOnlyTheGameData()
    {
        // Act
        IReadOnlyList<string> folders = GameDataFolders.GameOnly(GameFolder);

        // Assert
        Assert.Equal([GameFolder + @"\data"], folders);
    }

    private void ModWithData(string mod)
    {
        _fileSystemMock.DirectoryExists(mod).Returns(true);
        _fileSystemMock.DirectoryExists(mod + @"\data").Returns(true);
        _fileSystemMock.EnumerateFiles(mod).Returns([]);
    }
}
