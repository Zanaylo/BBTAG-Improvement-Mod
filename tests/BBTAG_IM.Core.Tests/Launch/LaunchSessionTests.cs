using BBTAG_IM.Core.Launch;

namespace BBTAG_IM.Core.Tests.Launch;

public class LaunchSessionTests
{
    private static readonly LaunchSession Session = new(
        @"D:\Steam\steamapps\common\BBTAG",
        @"D:\BBTAG_IM\logs\BBTAG_IM.log",
        [@"D:\Mods\Overhaul", @"D:\Mods\a=b"]);

    [Fact]
    public void Parse_RoundTripsSerializedSession()
    {
        var parsed = LaunchSession.Parse(Session.Serialize());

        Assert.Equal(Session.GameDirectory, parsed.GameDirectory);
        Assert.Equal(Session.LogPath, parsed.LogPath);
        Assert.Equal(Session.ModFolders, parsed.ModFolders);
    }

    [Fact]
    public void Parse_IgnoresBlankLinesAndUnknownKeys()
    {
        var parsed = LaunchSession.Parse("\r\ngame=C:\\Game\r\nfuture=1\r\nlog=C:\\log.txt\r\n\r\nmod=C:\\Mod\r\n");

        Assert.Equal(@"C:\Game", parsed.GameDirectory);
        Assert.Equal(@"C:\log.txt", parsed.LogPath);
        Assert.Equal([@"C:\Mod"], parsed.ModFolders);
    }

    [Theory]
    [InlineData("log=C:\\log.txt\nmod=C:\\Mod")]
    [InlineData("game=C:\\Game\nmod=C:\\Mod")]
    public void Parse_RejectsSessionWithoutRequiredKeys(string text)
    {
        Assert.Throws<InvalidLaunchSessionException>(() => LaunchSession.Parse(text));
    }
}
