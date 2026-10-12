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
    public void From_StripsGameRootFromAbsolutePaths(string requested, string expected)
    {
        Assert.Equal(expected, GamePathKey.From(requested, GameRoot));
    }

    [Theory]
    [InlineData("data/char/char_adh_col.pac", @"data\char\char_adh_col.pac")]
    [InlineData(@".\data\char\char_adh_col.pac", @"data\char\char_adh_col.pac")]
    [InlineData(@"./data//localize/eng_idlist.txt", @"data\localize\eng_idlist.txt")]
    public void From_NormalisesRelativePaths(string requested, string expected)
    {
        Assert.Equal(expected, GamePathKey.From(requested, GameRoot));
    }

    [Fact]
    public void From_AcceptsGameRootWithTrailingSeparator()
    {
        Assert.Equal(@"data\a.pac", GamePathKey.From(GameRoot + @"\data\a.pac", GameRoot + @"\"));
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
    public void From_ReturnsNullForPathsOutsideTheGame(string? requested)
    {
        Assert.Null(GamePathKey.From(requested, GameRoot));
    }
}
