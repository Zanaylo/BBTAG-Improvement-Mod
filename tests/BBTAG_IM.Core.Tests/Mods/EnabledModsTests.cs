using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class EnabledModsTests
{
    private const string ModsFolder = @"D:\BBTAG\mods";
    private const string ImprovementMod = @"D:\BBTAG\mods\BBTAG-Improvement-Mod";
    private const string Overhaul = @"D:\BBTAG\mods\BBTAG-OverHaul";
    private const string Costumes = @"D:\BBTAG\mods\Costumes";
    private const string LauncherSettingsPath = @"D:\BBTAG\BBTAG_IM.json";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public EnabledModsTests()
    {
        _fileSystemMock.DirectoryExists(ModsFolder).Returns(true);
        _fileSystemMock.EnumerateDirectories(ModsFolder).Returns([ImprovementMod, Overhaul, Costumes]);

        foreach (string mod in new[] { ImprovementMod, Overhaul, Costumes })
        {
            _fileSystemMock.DirectoryExists(mod).Returns(true);
            _fileSystemMock.DirectoryExists(mod + @"\data").Returns(true);
            _fileSystemMock.EnumerateFiles(mod).Returns([]);
        }
    }

    [Fact]
    public void Of_NoLauncherSettings_ListsEveryModInNameOrder()
    {
        // Arrange
        _fileSystemMock.FileExists(LauncherSettingsPath).Returns(false);

        // Act
        IReadOnlyList<ModFolder> mods = EnabledMods.Of(ImprovementMod, _fileSystemMock);

        // Assert
        Assert.Equal(["BBTAG-Improvement-Mod", "BBTAG-OverHaul", "Costumes"], mods.Select(mod => mod.Name));
    }

    [Fact]
    public void Of_ModTurnedOffInTheLauncher_LeavesItOut()
    {
        // Arrange
        _fileSystemMock.FileExists(LauncherSettingsPath).Returns(true);
        _fileSystemMock.ReadAllText(LauncherSettingsPath).Returns("""{ "DisabledMods": ["Costumes"] }""");

        // Act
        IReadOnlyList<ModFolder> mods = EnabledMods.Of(ImprovementMod, _fileSystemMock);

        // Assert
        Assert.Equal(["BBTAG-Improvement-Mod", "BBTAG-OverHaul"], mods.Select(mod => mod.Name));
    }

    [Fact]
    public void IsOn_ModTurnedOffInTheLauncher_ReturnsFalse()
    {
        // Arrange
        _fileSystemMock.FileExists(LauncherSettingsPath).Returns(true);
        _fileSystemMock.ReadAllText(LauncherSettingsPath).Returns("""{ "DisabledMods": ["DEV"] }""");

        // Act
        bool on = EnabledMods.IsOn("DEV", ImprovementMod, _fileSystemMock);

        // Assert
        Assert.False(on);
    }

    [Fact]
    public void IsOn_ModNotTurnedOff_ReturnsTrue()
    {
        // Arrange
        _fileSystemMock.FileExists(LauncherSettingsPath).Returns(true);
        _fileSystemMock.ReadAllText(LauncherSettingsPath).Returns("""{ "DisabledMods": ["Costumes"] }""");

        // Act
        bool on = EnabledMods.IsOn("DEV", ImprovementMod, _fileSystemMock);

        // Assert
        Assert.True(on);
    }

    [Fact]
    public void IsOn_NoLauncherSettings_ReturnsTrue()
    {
        // Arrange
        _fileSystemMock.FileExists(LauncherSettingsPath).Returns(false);

        // Act
        bool on = EnabledMods.IsOn("DEV", ImprovementMod, _fileSystemMock);

        // Assert
        Assert.True(on);
    }
}
