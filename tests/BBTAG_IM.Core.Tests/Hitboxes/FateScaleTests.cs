using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes;

namespace BBTAG_IM.Core.Tests.Hitboxes;

public class FateScaleTests
{
    private const int Precision = 5;

    [Theory]
    [InlineData(Fate.UnderNight, 1.05f)]
    [InlineData(Fate.Persona, 0.95f)]
    [InlineData(Fate.BlazBlue, 1f)]
    [InlineData(Fate.RWBY, 1f)]
    [InlineData(Fate.MeltyBlood, 1f)]
    public void Of_Fate_ReturnsTheSizeTheGameDrawsItsCharactersAt(Fate fate, float expected)
    {
        // Act
        float scale = FateScale.Of(fate);

        // Assert
        Assert.Equal(expected, scale, Precision);
    }

    [Fact]
    public void Of_UnknownFate_KeepsTheBoxesAtTheirSize()
    {
        // Act
        float scale = FateScale.Of(null);

        // Assert
        Assert.Equal(1f, scale, Precision);
    }
}
