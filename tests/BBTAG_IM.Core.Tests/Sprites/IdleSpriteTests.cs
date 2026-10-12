using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Tests.Sprites;

public class IdleSpriteTests
{
    [Fact]
    public void Pick_ArchiveHoldingAnotherCharactersIdleFirst_PicksTheIdleOfTheMostCommonPrefix()
    {
        // Arrange
        FpacEntry[] entries =
        [
            Entry("mk000_00.hip"),
            Entry("no000_00.hip"),
            Entry("no000_01.hip"),
            Entry("no010_00.hip"),
            Entry("ef_no_flash.hip"),
        ];

        // Act
        FpacEntry? idle = IdleSprite.Pick(entries);

        // Assert
        Assert.Equal("no000_00.hip", idle?.Name);
    }

    [Fact]
    public void Pick_PrefixWithCapitalLetters_FindsItsIdle()
    {
        // Arrange
        FpacEntry[] entries = [Entry("Hyd000_00.hip"), Entry("Hyd001_00.hip"), Entry("hyd900_00.hip")];

        // Act
        FpacEntry? idle = IdleSprite.Pick(entries);

        // Assert
        Assert.Equal("Hyd000_00.hip", idle?.Name);
    }

    [Fact]
    public void Pick_NoIdleForTheMostCommonPrefix_ReturnsNull()
    {
        // Arrange
        FpacEntry[] entries = [Entry("rg001_00.hip"), Entry("rg002_00.hip"), Entry("mk000_00.hip")];

        // Act
        FpacEntry? idle = IdleSprite.Pick(entries);

        // Assert
        Assert.Null(idle);
    }

    [Fact]
    public void Pick_NoSprites_ReturnsNull()
    {
        // Act
        FpacEntry? idle = IdleSprite.Pick([Entry("readme.txt")]);

        // Assert
        Assert.Null(idle);
    }

    private static FpacEntry Entry(string name) => new(name, 0, 1);
}
