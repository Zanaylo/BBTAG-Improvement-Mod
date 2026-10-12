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
    public void Parse_TextWithBlankLinesAndUnknownKeys_IgnoresThem()
    {
        // Act
        LaunchSession parsed = LaunchSession.Parse("\r\ngame=C:\\Game\r\nfuture=1\r\nlog=C:\\log.txt\r\n\r\nmod=C:\\Mod\r\n");

        // Assert
        Assert.Equal((@"C:\Game", @"C:\log.txt"), (parsed.GameDirectory, parsed.LogPath));
        Assert.Equal([@"C:\Mod"], parsed.ModFolders);
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
