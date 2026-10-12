using BBTAG_IM.Core.Hitboxes;

namespace BBTAG_IM.Core.Tests.Hitboxes;

public class HitboxColorsTests
{
    [Theory]
    [InlineData(0.5f, 0x7F3CDC3Cu)]
    [InlineData(0f, 0x003CDC3Cu)]
    [InlineData(2f, 0xFF3CDC3Cu)]
    public void WithAlpha_Alpha_ReplacesTheAlphaOfTheColor(float alpha, uint expected)
    {
        // Act
        uint color = HitboxColors.WithAlpha(HitboxColors.Hurtbox, alpha);

        // Assert
        Assert.Equal(expected, color);
    }
}
