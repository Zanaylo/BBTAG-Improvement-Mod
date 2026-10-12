using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Tests.Mods;

public class GamePathKeyTests
{
    private const string GameRoot = @"D:\Steam\steamapps\common\BBTAG";

    [Theory]
    [InlineData(@"D:\Steam\steamapps\common\BBTAG\data\char\char_adh_col.pac", @"data\char\char_adh_col.pac")]
    [InlineData(@"d:\steam\STEAMAPPS\common\bbtag\Data\Char\Char_Adh_Col.pac", @"Data\Char\Char_Adh_Col.pac")]
    [InlineData(@"D:/Steam/steamapps/common/BBTAG/data/bg/bg_riverbed.pac", @"data\bg\bg_riverbed.pac")]
    [InlineData(@"\\?\D:\Steam\steamapps\common\BBTAG\data\etc\cockpit_obj.pac", @"data\etc\cockpit_obj.pac")]
    [InlineData(@"D:\Steam\steamapps\common\BBTAG\data\.\char\..\bg\\bg_riverbed.pac", @"data\bg\bg_riverbed.pac")]
    public void From_AbsolutePathInsideTheGame_ReturnsThePathRelativeToTheGame(string requested, string expected)
    {
        // Act
        string? key = GamePathKey.From(requested, GameRoot);

        // Assert
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData("data/char/char_adh_col.pac", @"data\char\char_adh_col.pac")]
    [InlineData(@".\data\char\char_adh_col.pac", @"data\char\char_adh_col.pac")]
    [InlineData(@"./data//localize/eng_idlist.txt", @"data\localize\eng_idlist.txt")]
    public void From_RelativePath_ReturnsTheNormalisedPath(string requested, string expected)
    {
        // Act
        string? key = GamePathKey.From(requested, GameRoot);

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    public void From_GameRootWithTrailingSeparator_ReturnsThePathRelativeToTheGame()
    {
        // Act
        string? key = GamePathKey.From(GameRoot + @"\data\a.pac", GameRoot + @"\");

        // Assert
        Assert.Equal(@"data\a.pac", key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"C:\Windows\System32\kernel32.dll")]
    [InlineData(@"D:\Steam\steamapps\common\BBTAG2\data\a.pac")]
    [InlineData(@"D:\Steam\steamapps\common\BBTAG")]
    [InlineData(@"..\outside\data\a.pac")]
    [InlineData(@"data\..\..\a.pac")]
    [InlineData(@"\\server\share\data\a.pac")]
    [InlineData(@"\data\a.pac")]
    [InlineData(@"C:data\a.pac")]
    [InlineData(@"\\.\pipe\steam")]
    [InlineData("CONIN$")]
    public void From_PathOutsideTheGameFolders_ReturnsNull(string? requested)
    {
        // Act
        string? key = GamePathKey.From(requested, GameRoot);

        // Assert
        Assert.Null(key);
    }
}
