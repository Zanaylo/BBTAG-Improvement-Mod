using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class CharacterFileCheckTests
{
    private const string Effects = @"data\eff3d\eff3d_mko.pac";
    private const string Sprites = @"data\char\char_mko_img.pac";
    private const string HighResolutionSprites = @"data\char2x\char_mko_img.pac";

    private readonly IGameFiles _gameFilesMock = Substitute.For<IGameFiles>();

    [Fact]
    public void Run_CharacterWithEveryFile_ReportsNothing()
    {
        // Arrange
        _gameFilesMock.Has(Arg.Any<string>()).Returns(true);
        CharacterFileCheck check = CreateCharacterFileCheck();

        // Act
        IReadOnlyList<CharacterFileReport> reports = check.Run(["mko"]);

        // Assert
        Assert.Empty(reports);
    }

    [Fact]
    public void Run_CharacterMissingAFile_ReportsThatFile()
    {
        // Arrange
        _gameFilesMock.Has(Arg.Any<string>()).Returns(true);
        _gameFilesMock.Has(Effects).Returns(false);
        CharacterFileCheck check = CreateCharacterFileCheck();

        // Act
        IReadOnlyList<CharacterFileReport> reports = check.Run(["mko"]);

        // Assert
        CharacterFileReport report = Assert.Single(reports);
        Assert.Equal(("mko", Effects, CharacterFileRules.For("mko").Count), (report.Code, Assert.Single(report.Missing), report.Required));
    }

    [Fact]
    public void Run_SpritesOnlyInAnotherSpriteFolder_ReportsNothing()
    {
        // Arrange
        _gameFilesMock.Has(Arg.Any<string>()).Returns(true);
        _gameFilesMock.Has(Sprites).Returns(false);
        _gameFilesMock.Has(HighResolutionSprites).Returns(true);
        CharacterFileCheck check = CreateCharacterFileCheck();

        // Act
        IReadOnlyList<CharacterFileReport> reports = check.Run(["mko"]);

        // Assert
        Assert.Empty(reports);
    }

    [Fact]
    public void Run_TwoIncompleteCharacters_ReportsBothInTheGivenOrder()
    {
        // Arrange
        _gameFilesMock.Has(Arg.Any<string>()).Returns(false);
        CharacterFileCheck check = CreateCharacterFileCheck();

        // Act
        IReadOnlyList<CharacterFileReport> reports = check.Run(["mko", "mhi"]);

        // Assert
        Assert.Equal(["mko", "mhi"], reports.Select(report => report.Code));
    }

    public CharacterFileCheck CreateCharacterFileCheck() => new(_gameFilesMock);
}
