using BBTAG_IM.Core.Hantei.Games;
using BBTAG_IM.Core.Porting;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Porting;

public class PortJobTests
{
    private readonly IHanteiGameFiles _files = Substitute.For<IHanteiGameFiles>();

    private PortJob Job(string title, string letter, string code, string name) =>
        new(new HanteiGame(title, @"X:\game", _files, letter), new HanteiCharacter(code, name), "Others");

    [Theory]
    [InlineData("m", "chr004", "xm4")]
    [InlineData("u", "chr027", "xur")]
    public void Code_Character_JoinsGameAndNumber(string letter, string character, string expected)
    {
        // Arrange
        PortJob job = Job("Game", letter, character, "Name");

        // Act
        string code = job.Code;

        // Assert
        Assert.Equal(expected, code);
    }

    [Fact]
    public void ModName_PunctuatedName_KeepsLettersAndDigits()
    {
        // Arrange
        PortJob job = Job("Game", "m", "chr020", "Neco-Arc 2");

        // Act
        string name = job.ModName;

        // Assert
        Assert.Equal("FB-NecoArc2", name);
    }

    [Fact]
    public void Arguments_Character_AsksForARosterEntry()
    {
        // Arrange
        PortJob job = Job("Game", "m", "chr004", "Kohaku");

        // Act
        IReadOnlyList<string> arguments = job.Arguments(@"X:\bbtag\data", @"X:\bbtag\mods", @"X:\db");

        // Assert
        Assert.Equal(
            ["port", @"X:\game", "chr004", @"X:\bbtag\data", "uhy", PortJob.ImagePrefix, "--foreign", "--roster", "xm4", "--name", "Kohaku", "--series", "Others", "--out", @"X:\bbtag\mods\FB-Kohaku", "--db", @"X:\db"],
            arguments);
    }
}
