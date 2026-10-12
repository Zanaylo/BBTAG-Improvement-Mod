using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Tests.Settings;

public class ImprovementSettingsTests
{
    [Fact]
    public void Constructor_NewSettings_ShowHitboxesAndLeaveTheFrameLimiterOff()
    {
        // Act
        ImprovementSettings settings = new();

        // Assert
        Assert.Equal((true, false), (settings.HitboxViewer, settings.FrameLimiter));
    }

    [Fact]
    public void Constructor_NewSettings_ShowEveryBoxTypeAndTheOrigins()
    {
        // Act
        ImprovementSettings settings = new();

        // Assert
        Assert.Equal((true, true, true), (settings.Hurtboxes.Shown, settings.Hitboxes.Shown, settings.HitboxOrigins));
        Assert.Equal((BoxStyle.DefaultFill, BoxStyle.DefaultOutline), (settings.Hitboxes.Fill, settings.Hitboxes.Outline));
    }

    [Theory]
    [InlineData(BoxKind.Hurtbox)]
    [InlineData(BoxKind.Hitbox)]
    public void StyleOf_Kind_ReturnsTheStyleOfThatKind(BoxKind kind)
    {
        // Arrange
        ImprovementSettings settings = new();

        // Act
        BoxStyle style = settings.StyleOf(kind);

        // Assert
        Assert.Same(kind == BoxKind.Hitbox ? settings.Hitboxes : settings.Hurtboxes, style);
    }

    [Fact]
    public void PaletteFor_CharacterNameInAnotherCase_FindsTheChosenPalette()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 3, "Minato");

        // Act
        string? palette = settings.PaletteFor("aegis", 3);

        // Assert
        Assert.Equal("Minato", palette);
    }

    [Fact]
    public void PaletteFor_ColorWithoutChoice_ReturnsNull()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 3, "Minato");

        // Act
        string? palette = settings.PaletteFor("Aegis", 4);

        // Assert
        Assert.Null(palette);
    }

    [Fact]
    public void ChoosePalette_AnotherPaletteForTheSameColor_ReplacesTheChoice()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 3, "Minato");

        // Act
        settings.ChoosePalette("Aegis", 3, "Shadow Aegis");

        // Assert
        Assert.Equal("Shadow Aegis", settings.PaletteFor("Aegis", 3));
    }

    [Fact]
    public void ChoosePalette_NoPaletteForTheLastColor_ForgetsTheCharacter()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 3, "Minato");

        // Act
        settings.ChoosePalette("Aegis", 3, null);

        // Assert
        Assert.Empty(settings.Palettes);
    }

    [Fact]
    public void ChoosePalette_NoPaletteForOneOfTwoColors_KeepsTheOtherColor()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 3, "Minato");
        settings.ChoosePalette("Aegis", 5, "Gold");

        // Act
        settings.ChoosePalette("Aegis", 3, null);

        // Assert
        Assert.Equal(new Dictionary<int, string> { [5] = "Gold" }, settings.Palettes["Aegis"]);
    }

    [Fact]
    public void ColorOf_PaletteInTwoColors_ReturnsTheLowestColor()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 7, "Minato");
        settings.ChoosePalette("Aegis", 3, "minato");

        // Act
        int? color = settings.ColorOf("aegis", "Minato");

        // Assert
        Assert.Equal(3, color);
    }

    [Fact]
    public void ColorOf_PaletteInNoColor_ReturnsNull()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 3, "Gold");

        // Act
        int? color = settings.ColorOf("Aegis", "Minato");

        // Assert
        Assert.Null(color);
    }

    [Fact]
    public void AssignPalette_NewColor_MovesThePaletteOutOfEveryOtherColor()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 3, "Minato");
        settings.ChoosePalette("Aegis", 5, "Minato");
        settings.ChoosePalette("Aegis", 6, "Gold");

        // Act
        settings.AssignPalette("Aegis", "minato", 9);

        // Assert
        Assert.Equal(new Dictionary<int, string> { [6] = "Gold", [9] = "minato" }, settings.Palettes["Aegis"]);
    }

    [Fact]
    public void AssignPalette_ColorHeldByAnotherPalette_ReplacesIt()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 6, "Gold");

        // Act
        settings.AssignPalette("Aegis", "Minato", 6);

        // Assert
        Assert.Equal(new Dictionary<int, string> { [6] = "Minato" }, settings.Palettes["Aegis"]);
    }

    [Fact]
    public void AssignPalette_NoColor_RemovesThePaletteAndTheEmptyCharacter()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 3, "Minato");

        // Act
        settings.AssignPalette("Aegis", "Minato", null);

        // Assert
        Assert.Empty(settings.Palettes);
    }

    [Fact]
    public void AssignPalette_NoColorForACharacterWithoutChoices_ChangesNothing()
    {
        // Arrange
        ImprovementSettings settings = new();

        // Act
        settings.AssignPalette("Aegis", "Minato", null);

        // Assert
        Assert.Empty(settings.Palettes);
    }

    [Fact]
    public void HighestColor_CharacterWithChoices_ReturnsTheHighestChosenColor()
    {
        // Arrange
        ImprovementSettings settings = new();
        settings.ChoosePalette("Aegis", 42, "Minato");
        settings.ChoosePalette("Aegis", 3, "Gold");

        // Act
        int highest = settings.HighestColor("aegis");

        // Assert
        Assert.Equal(42, highest);
    }

    [Fact]
    public void HighestColor_CharacterWithoutChoices_ReturnsZero()
    {
        // Arrange
        ImprovementSettings settings = new();

        // Act
        int highest = settings.HighestColor("Aegis");

        // Assert
        Assert.Equal(0, highest);
    }
}
