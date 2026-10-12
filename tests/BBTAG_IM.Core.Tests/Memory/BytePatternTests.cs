using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Tests.Memory;

public class BytePatternTests
{
    [Fact]
    public void FindAll_PatternWithWildcards_ReturnsEveryMatchOffset()
    {
        // Arrange
        BytePattern pattern = BytePattern.Parse("E8 ?? ?? C3");
        byte[] bytes = [0x90, 0xE8, 0x01, 0x02, 0xC3, 0xE8, 0xFF, 0xFF, 0xC3];

        // Act
        IReadOnlyList<int> offsets = pattern.FindAll(bytes);

        // Assert
        Assert.Equal([1, 5], offsets);
    }

    [Fact]
    public void FindAll_FixedByteDiffers_SkipsThatPlace()
    {
        // Arrange
        BytePattern pattern = BytePattern.Parse("E8 ?? C3");
        byte[] bytes = [0xE8, 0x00, 0xC2, 0xE8];

        // Act
        IReadOnlyList<int> offsets = pattern.FindAll(bytes);

        // Assert
        Assert.Empty(offsets);
    }

    [Fact]
    public void FindAll_PatternStartingWithWildcard_MatchesOnTheFirstFixedByte()
    {
        // Arrange
        BytePattern pattern = BytePattern.Parse("?? 8B C8");
        byte[] bytes = [0x8B, 0xC8, 0x55, 0x8B, 0xC8];

        // Act
        IReadOnlyList<int> offsets = pattern.FindAll(bytes);

        // Assert
        Assert.Equal([2], offsets);
    }

    [Fact]
    public void FindAll_MatchWouldRunPastTheEnd_IsNotReported()
    {
        // Arrange
        BytePattern pattern = BytePattern.Parse("8B C8 ??");
        byte[] bytes = [0x00, 0x8B, 0xC8];

        // Act
        IReadOnlyList<int> offsets = pattern.FindAll(bytes);

        // Assert
        Assert.Empty(offsets);
    }

    [Fact]
    public void Parse_LowerCaseBytes_ReadsThemAsHex()
    {
        // Arrange
        byte[] bytes = [0xAB, 0xCD];

        // Act
        BytePattern pattern = BytePattern.Parse("ab cd");

        // Assert
        Assert.Equal([0], pattern.FindAll(bytes));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?? ??")]
    [InlineData("E8 XY")]
    [InlineData("E81")]
    public void Parse_InvalidText_ThrowsArgumentException(string text)
    {
        // Act
        Exception? exception = Record.Exception(() => BytePattern.Parse(text));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }
}
