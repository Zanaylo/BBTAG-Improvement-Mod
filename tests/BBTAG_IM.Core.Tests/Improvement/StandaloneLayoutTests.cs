using BBTAG_IM.Core.Improvement;
using BBTAG_IM.Core.IO;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Improvement;

public class StandaloneLayoutTests
{
    private const string GamePath = @"C:\Steam\BBTAG\BBTAG.exe";
    private const string GameFolder = @"C:\Steam\BBTAG";
    private const string SettingsPath = @"C:\Steam\BBTAG\BBTAG_IM.json";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void For_GameExecutable_PutsTheModInTheModsFolderOfTheGame()
    {
        // Act
        StandaloneLayout layout = StandaloneLayout.For(GamePath, _fileSystemMock);

        // Assert
        Assert.Equal(GameFolder, layout.GameFolder);
        Assert.Equal(@"C:\Steam\BBTAG\mods\BBTAG-Improvement-Mod", layout.ModFolder);
    }

    [Fact]
    public void For_NoSettingsFile_WritesNoLogs()
    {
        // Arrange
        _fileSystemMock.FileExists(SettingsPath).Returns(false);

        // Act
        StandaloneLayout layout = StandaloneLayout.For(GamePath, _fileSystemMock);

        // Assert
        Assert.Empty(layout.LogFolder);
    }

    [Fact]
    public void For_LogsTurnedOff_WritesNoLogs()
    {
        // Arrange
        _fileSystemMock.FileExists(SettingsPath).Returns(true);
        _fileSystemMock.ReadAllText(SettingsPath).Returns("""{ "WriteLogs": false }""");

        // Act
        StandaloneLayout layout = StandaloneLayout.For(GamePath, _fileSystemMock);

        // Assert
        Assert.Empty(layout.LogFolder);
    }

    [Fact]
    public void For_LogsTurnedOn_WritesThemInTheLogsFolderOfTheGame()
    {
        // Arrange
        _fileSystemMock.FileExists(SettingsPath).Returns(true);
        _fileSystemMock.ReadAllText(SettingsPath).Returns("""{ "WriteLogs": true }""");

        // Act
        StandaloneLayout layout = StandaloneLayout.For(GamePath, _fileSystemMock);

        // Assert
        Assert.Equal(@"C:\Steam\BBTAG\logs", layout.LogFolder);
    }
}
