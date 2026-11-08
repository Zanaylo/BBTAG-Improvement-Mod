using BBTAG_IM.Core.Launch;

namespace BBTAG_IM.Core.Tests.Launch;

public class LaunchSessionTests
{
    [Fact]
    public void Parse_SerializedSession_ReturnsTheSameValues()
    {
        // Arrange
        LaunchSession session = new(@"D:\Steam\steamapps\common\BBTAG", @"D:\BBTAG_IM\logs\BBTAG_IM.log", [@"D:\Mods\Overhaul", @"D:\Mods\a=b"]);

        // Act
        LaunchSession parsed = LaunchSession.Parse(session.Serialize());

        // Assert
        Assert.Equal((session.GameDirectory, session.LogPath), (parsed.GameDirectory, parsed.LogPath));
        Assert.Equal(session.ModFolders, parsed.ModFolders);
    }

    [Fact]
    public void Parse_SessionWaitingForADebugger_KeepsTheChoice()
    {
        // Arrange
        LaunchSession session = new(@"D:\Steam\steamapps\common\BBTAG", @"D:\BBTAG_IM\logs\BBTAG_IM.log", [], WaitForDebugger: true);

        // Act
        LaunchSession parsed = LaunchSession.Parse(session.Serialize());

        // Assert
        Assert.True(parsed.WaitForDebugger);
    }

    [Fact]
    public void Parse_TextWithoutTheDebuggerChoice_DoesNotWait()
    {
        // Act
        LaunchSession parsed = LaunchSession.Parse("game=C:\\Game\nlog=C:\\log.txt\n");

        // Assert
        Assert.False(parsed.WaitForDebugger);
    }

    [Fact]
    public void Parse_TextWithBlankLinesAndUnknownKeys_IgnoresThem()
    {
        // Act
        LaunchSession parsed = LaunchSession.Parse("\r\ngame=C:\\Game\r\nfuture=1\r\nlog=C:\\log.txt\r\n\r\nmod=C:\\Mod\r\n");

        // Assert
        Assert.Equal((@"C:\Game", @"C:\log.txt"), (parsed.GameDirectory, parsed.LogPath));
        Assert.Equal([@"C:\Mod"], parsed.ModFolders);
    }

    [Fact]
    public void Parse_SessionWritingLogs_KeepsTheChoice()
    {
        // Arrange
        LaunchSession session = new(@"D:\Steam\steamapps\common\BBTAG", @"D:\BBTAG_IM\logs\BBTAG_IM.log", [], WriteLogs: true);

        // Act
        LaunchSession parsed = LaunchSession.Parse(session.Serialize());

        // Assert
        Assert.True(parsed.WriteLogs);
    }

    [Fact]
    public void Parse_SessionNotAskingForLogs_LeavesThemOff()
    {
        // Arrange
        LaunchSession session = new(@"D:\Steam\steamapps\common\BBTAG", @"D:\BBTAG_IM\logs\BBTAG_IM.log", []);

        // Act
        LaunchSession parsed = LaunchSession.Parse(session.Serialize());

        // Assert
        Assert.False(parsed.WriteLogs);
    }

    [Fact]
    public void PluginLogFolder_LogsOn_IsTheFolderOfTheLog()
    {
        // Arrange
        LaunchSession session = new(@"D:\Steam\BBTAG", @"D:\BBTAG_IM\logs\BBTAG_IM.log", [], WriteLogs: true);

        // Act
        string folder = session.PluginLogFolder;

        // Assert
        Assert.Equal(@"D:\BBTAG_IM\logs", folder);
    }

    [Fact]
    public void PluginLogFolder_LogsOff_IsEmpty()
    {
        // Arrange
        LaunchSession session = new(@"D:\Steam\BBTAG", @"D:\BBTAG_IM\logs\BBTAG_IM.log", []);

        // Act
        string folder = session.PluginLogFolder;

        // Assert
        Assert.Empty(folder);
    }

    [Theory]
    [InlineData("log=C:\\log.txt\nmod=C:\\Mod")]
    [InlineData("game=C:\\Game\nmod=C:\\Mod")]
    public void Parse_TextWithoutGameOrLog_ThrowsInvalidLaunchSession(string text)
    {
        // Act
        Exception? exception = Record.Exception(() => LaunchSession.Parse(text));

        // Assert
        Assert.IsType<InvalidLaunchSessionException>(exception);
    }
}
