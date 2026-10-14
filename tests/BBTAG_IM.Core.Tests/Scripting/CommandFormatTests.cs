using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Tests.Scripting;

public class CommandFormatTests
{
    [Fact]
    public void Parse_TextThenRepeatedIntegers_ReturnsOneTextAndEachInteger()
    {
        // Arrange
        string format = "32s2iI";

        // Act
        IReadOnlyList<CommandArgument> arguments = CommandFormat.Parse(format);

        // Assert
        Assert.Equal([CommandArgument.Text(32), CommandArgument.Integer, CommandArgument.Integer, CommandArgument.Unsigned], arguments);
    }

    [Fact]
    public void Parse_UnsupportedLetter_ThrowsInvalidScriptDatabaseException()
    {
        // Arrange
        string format = "2h";

        // Act
        Exception? exception = Record.Exception(() => CommandFormat.Parse(format));

        // Assert
        Assert.IsType<InvalidScriptDatabaseException>(exception);
    }

    [Fact]
    public void ForSize_SizeMadeOfWholeIntegers_ReturnsIntegers()
    {
        // Arrange
        int size = 12;

        // Act
        IReadOnlyList<CommandArgument> arguments = CommandFormat.ForSize(size);

        // Assert
        Assert.Equal([CommandArgument.Integer, CommandArgument.Integer], arguments);
    }

    [Fact]
    public void ForSize_SizeNotMadeOfIntegers_ReturnsOneTextField()
    {
        // Arrange
        int size = 7;

        // Act
        IReadOnlyList<CommandArgument> arguments = CommandFormat.ForSize(size);

        // Assert
        Assert.Equal([CommandArgument.Text(3)], arguments);
    }

    [Fact]
    public void SizeOf_Arguments_AddsTheCommandId()
    {
        // Arrange
        IReadOnlyList<CommandArgument> arguments = CommandFormat.Parse("32si");

        // Act
        int size = CommandFormat.SizeOf(arguments);

        // Assert
        Assert.Equal(40, size);
    }
}
