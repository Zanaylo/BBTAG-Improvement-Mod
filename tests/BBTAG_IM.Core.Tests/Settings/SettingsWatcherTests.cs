using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Settings;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Settings;

public class SettingsWatcherTests
{
    private const string ModDirectory = @"D:\BBTAG\mods\BBTAG-Improvement-Mod";
    private const string SettingsPath = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\ImprovementMod.json";

    private static readonly DateTime FirstWrite = new(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SecondWrite = FirstWrite.AddSeconds(5);

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public SettingsWatcher<ImprovementSettings> CreateSettingsWatcher() =>
        new(SettingsStores.Improvement(_fileSystemMock, ModDirectory), _fileSystemMock);

    [Fact]
    public void Current_BeforeAnyRefresh_HoldsTheFileContents()
    {
        // Arrange
        FileWritten(FirstWrite, "{ \"FrameLimiter\": true }");

        // Act
        SettingsWatcher<ImprovementSettings> watcher = CreateSettingsWatcher();

        // Assert
        Assert.True(watcher.Current.FrameLimiter);
    }

    [Fact]
    public void Refresh_FileNotWrittenAgain_ReturnsFalseAndKeepsTheSettings()
    {
        // Arrange
        FileWritten(FirstWrite, "{ \"FrameLimiter\": true }");
        SettingsWatcher<ImprovementSettings> watcher = CreateSettingsWatcher();
        ImprovementSettings before = watcher.Current;

        // Act
        bool changed = watcher.Refresh();

        // Assert
        Assert.False(changed);
        Assert.Same(before, watcher.Current);
    }

    [Fact]
    public void Refresh_FileWrittenAgain_LoadsTheNewSettings()
    {
        // Arrange
        FileWritten(FirstWrite, "{ \"HitboxViewer\": true }");
        SettingsWatcher<ImprovementSettings> watcher = CreateSettingsWatcher();
        FileWritten(SecondWrite, "{ \"HitboxViewer\": false }");

        // Act
        bool changed = watcher.Refresh();

        // Assert
        Assert.True(changed);
        Assert.False(watcher.Current.HitboxViewer);
    }

    [Fact]
    public void Refresh_FileCaughtHalfWritten_KeepsThePreviousSettings()
    {
        // Arrange
        FileWritten(FirstWrite, "{ \"FrameLimiter\": true }");
        SettingsWatcher<ImprovementSettings> watcher = CreateSettingsWatcher();
        FileWritten(SecondWrite, "{ \"FrameLimiter\": fa");

        // Act
        bool changed = watcher.Refresh();

        // Assert
        Assert.False(changed);
        Assert.True(watcher.Current.FrameLimiter);
    }

    [Fact]
    public void Refresh_FileFinishedAfterBeingCaughtHalfWritten_LoadsItOnTheNextRefresh()
    {
        // Arrange
        FileWritten(FirstWrite, "{ \"FrameLimiter\": true }");
        SettingsWatcher<ImprovementSettings> watcher = CreateSettingsWatcher();
        FileWritten(SecondWrite, "{ \"FrameLimiter\": fa");
        watcher.Refresh();
        FileWritten(SecondWrite, "{ \"FrameLimiter\": false }");

        // Act
        bool changed = watcher.Refresh();

        // Assert
        Assert.True(changed);
        Assert.False(watcher.Current.FrameLimiter);
    }

    [Fact]
    public void Change_ChoiceMadeInGame_SavesTheFileWithIt()
    {
        // Arrange
        FileWritten(FirstWrite, "{ \"HitboxViewer\": true }");
        SettingsWatcher<ImprovementSettings> watcher = CreateSettingsWatcher();
        string? written = null;
        _fileSystemMock.When(fileSystem => fileSystem.WriteAllText(SettingsPath, Arg.Any<string>())).Do(call => written = call.ArgAt<string>(1));

        // Act
        watcher.Change(settings => settings.HitboxViewer = false);

        // Assert
        Assert.Contains("\"HitboxViewer\": false", written);
    }

    [Fact]
    public void Refresh_AfterOwnChange_DoesNotReloadTheFileItJustWrote()
    {
        // Arrange
        FileWritten(FirstWrite, "{ \"HitboxViewer\": true }");
        SettingsWatcher<ImprovementSettings> watcher = CreateSettingsWatcher();
        _fileSystemMock.When(fileSystem => fileSystem.WriteAllText(SettingsPath, Arg.Any<string>()))
            .Do(call => FileWritten(SecondWrite, call.ArgAt<string>(1)));
        watcher.Change(settings => settings.HitboxViewer = false);

        // Act
        bool changed = watcher.Refresh();

        // Assert
        Assert.False(changed);
    }

    private void FileWritten(DateTime time, string text)
    {
        _fileSystemMock.FileExists(SettingsPath).Returns(true);
        _fileSystemMock.LastWriteTimeUtc(SettingsPath).Returns(time);
        _fileSystemMock.ReadAllText(SettingsPath).Returns(text);
    }
}
