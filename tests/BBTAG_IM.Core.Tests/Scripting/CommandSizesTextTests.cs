using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Tests.Scripting;

public class CommandSizesTextTests
{
    [Fact]
    public void Write_Sizes_WritesOneCommandPerLineInIdOrder()
    {
        // Arrange
        Dictionary<int, int> sizes = new() { [40019] = 8, [0] = 36 };

        // Act
        string text = CommandSizesText.Write(sizes);

        // Assert
        Assert.Equal("0\t36\n40019\t8\n", text);
    }

    [Fact]
    public void Read_WrittenSizes_ReturnsTheSameSizes()
    {
        // Arrange
        Dictionary<int, int> sizes = new() { [0] = 36, [40019] = 8 };

        // Act
        IReadOnlyDictionary<int, int> read = CommandSizesText.Read(CommandSizesText.Write(sizes));

        // Assert
        Assert.Equal(sizes, read);
    }

    [Fact]
    public void Read_WindowsLineEndsAndBlankLines_SkipsTheBlankLines()
    {
        // Act
        IReadOnlyDictionary<int, int> sizes = CommandSizesText.Read("0\t36\r\n\r\n9000\t8\r\n");

        // Assert
        Assert.Equal(new Dictionary<int, int> { [0] = 36, [9000] = 8 }, sizes);
    }
}
