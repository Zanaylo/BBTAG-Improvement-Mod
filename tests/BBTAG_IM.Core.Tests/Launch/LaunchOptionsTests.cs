using BBTAG_IM.Core.Launch;

namespace BBTAG_IM.Core.Tests.Launch;

public class LaunchOptionsTests
{
    [Fact]
    public void Parse_ReturnsDefaultsWithoutArguments()
    {
        var options = LaunchOptions.Parse([]);

        Assert.Null(options.GameDirectory);
        Assert.Empty(options.ModFolders);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    public void Parse_TreatsBarePathsAsModFoldersInOrder()
    {
        var options = LaunchOptions.Parse([@"D:\Mods\Overhaul", @"D:\Mods\Palettes"]);

        Assert.Equal([@"D:\Mods\Overhaul", @"D:\Mods\Palettes"], options.ModFolders);
    }

    [Theory]
    [InlineData("--mod")]
    [InlineData("-m")]
    public void Parse_ReadsModOption(string option)
    {
        var options = LaunchOptions.Parse([@"D:\Mods\First", option, @"D:\Mods\Second"]);

        Assert.Equal([@"D:\Mods\First", @"D:\Mods\Second"], options.ModFolders);
    }

    [Theory]
    [InlineData("--game")]
    [InlineData("-g")]
    public void Parse_ReadsGameOption(string option)
    {
        var options = LaunchOptions.Parse([option, @"D:\Steam\steamapps\common\BBTAG"]);

        Assert.Equal(@"D:\Steam\steamapps\common\BBTAG", options.GameDirectory);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void Parse_ReadsHelpOption(string option)
    {
        Assert.True(LaunchOptions.Parse([option]).ShowHelp);
    }

    [Theory]
    [InlineData("--game")]
    [InlineData("--mod")]
    public void Parse_RejectsOptionWithoutValue(string option)
    {
        Assert.Throws<LaunchOptionsException>(() => LaunchOptions.Parse([option]));
    }

    [Fact]
    public void Parse_RejectsUnknownOption()
    {
        var exception = Assert.Throws<LaunchOptionsException>(() => LaunchOptions.Parse(["--turbo"]));

        Assert.Contains("--turbo", exception.Message);
    }
}
