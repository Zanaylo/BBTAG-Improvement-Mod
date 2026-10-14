using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Tests.Scripting;

public class ScriptLiteralsTests
{
    [Fact]
    public void Text_QuoteAndHighByte_EscapesBoth()
    {
        // Arrange
        string text = "kk'é";

        // Act
        string literal = ScriptLiterals.Text(text);

        // Assert
        Assert.Equal(@"'kk\'\xe9'", literal);
    }

    [Fact]
    public void Number_NegativeHex_PutsTheSignBeforeThePrefix()
    {
        // Arrange
        int value = -16;

        // Act
        string literal = ScriptLiterals.Number(value, hex: true, unsigned: false);

        // Assert
        Assert.Equal("-0x10", literal);
    }

    [Fact]
    public void Number_Unsigned_WritesTheFullRange()
    {
        // Arrange
        int value = -1;

        // Act
        string literal = ScriptLiterals.Number(value, hex: false, unsigned: true);

        // Assert
        Assert.Equal("4294967295", literal);
    }
}
