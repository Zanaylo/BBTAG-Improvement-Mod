using BBTAG_IM.Core.Launch;

namespace BBTAG_IM.Core.Tests.Launch;

public class LaunchOptionsTests
{
    [Fact]
    public void Parse_NoArguments_ReturnsDefaults()
    {
        // Act
        LaunchOptions options = LaunchOptions.Parse([]);

        // Assert
        Assert.Null(options.GameDirectory);
        Assert.Empty(options.ModFolders);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    public void Parse_BarePaths_TreatsThemAsModFoldersInOrder()
    {
        // Act
        LaunchOptions options = LaunchOptions.Parse([@"D:\Mods\Overhaul", @"D:\Mods\Palettes"]);

        // Assert
        Assert.Equal([@"D:\Mods\Overhaul", @"D:\Mods\Palettes"], options.ModFolders);
    }

    [Theory]
    [InlineData("--mod")]
    [InlineData("-m")]
    public void Parse_ModOptionBetweenBarePaths_KeepsTheOrder(string option)
    {
        // Act
        LaunchOptions options = LaunchOptions.Parse([@"D:\Mods\First", option, @"D:\Mods\Second"]);

        // Assert
        Assert.Equal([@"D:\Mods\First", @"D:\Mods\Second"], options.ModFolders);
    }

    [Theory]
    [InlineData("--game")]
    [InlineData("-g")]
    public void Parse_GameOption_ReadsTheGameDirectory(string option)
    {
        // Act
        LaunchOptions options = LaunchOptions.Parse([option, @"D:\Steam\steamapps\common\BBTAG"]);

        // Assert
        Assert.Equal(@"D:\Steam\steamapps\common\BBTAG", options.GameDirectory);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void Parse_HelpOption_AsksForHelp(string option)
    {
        // Act
        LaunchOptions options = LaunchOptions.Parse([option]);

        // Assert
        Assert.True(options.ShowHelp);
    }

    [Theory]
    [InlineData("--game")]
    [InlineData("--mod")]
    public void Parse_OptionWithoutValue_ThrowsLaunchOptionsException(string option)
    {
        // Act
        Exception? exception = Record.Exception(() => LaunchOptions.Parse([option]));

        // Assert
        Assert.Equal($"{option} needs a folder after it.", Assert.IsType<LaunchOptionsException>(exception).Message);
    }

    [Fact]
    public void Parse_UnknownOption_ThrowsLaunchOptionsExceptionNamingIt()
    {
        // Act
        Exception? exception = Record.Exception(() => LaunchOptions.Parse(["--turbo"]));

        // Assert
        Assert.Equal("Unknown option --turbo. Run with --help to see the options.", Assert.IsType<LaunchOptionsException>(exception).Message);
    }
}
