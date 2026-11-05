using BBTAG_IM.Core.Scripting.Importing;

namespace BBTAG_IM.Core.Tests.Scripting.Importing;

public class BbcfFormatTests
{
    [Fact]
    public void For_IntegersThatFillTheSize_WritesOneLetterEach()
    {
        // Arrange
        string[] types = ["S32", "BOOL", "Enum"];

        // Act
        string? format = BbcfFormat.For(types, 16);

        // Assert
        Assert.Equal("iii", format);
    }

    [Fact]
    public void For_UnsignedInteger_WritesACapitalI()
    {
        // Arrange
        string[] types = ["U32"];

        // Act
        string? format = BbcfFormat.For(types, 8);

        // Assert
        Assert.Equal("I", format);
    }

    [Fact]
    public void For_Text_WritesItsLength()
    {
        // Arrange
        string[] types = ["C32BYTE", "S32"];

        // Act
        string? format = BbcfFormat.For(types, 40);

        // Assert
        Assert.Equal("32si", format);
    }

    [Fact]
    public void For_NoArguments_IsEmpty()
    {
        // Arrange
        string[] types = [];

        // Act
        string? format = BbcfFormat.For(types, 4);

        // Assert
        Assert.Equal(string.Empty, format);
    }

    [Fact]
    public void For_ArgumentsThatDoNotFillTheSize_ReturnsNull()
    {
        // Arrange
        string[] types = ["S32"];

        // Act
        string? format = BbcfFormat.For(types, 12);

        // Assert
        Assert.Null(format);
    }

    [Fact]
    public void For_ByteArgument_ReturnsNull()
    {
        // Arrange
        string[] types = ["U8", "U8", "U8", "U8"];

        // Act
        string? format = BbcfFormat.For(types, 8);

        // Assert
        Assert.Null(format);
    }
}
