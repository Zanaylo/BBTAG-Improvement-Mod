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
}
