using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Tests.IO;

public class OpenFileSelectionTests
{
    [Fact]
    public void Paths_OneFile_ReturnsItsFullPath()
    {
        // Arrange
        string answer = "C:\\Palettes\\Minato.impl\0\0";

        // Act
        IReadOnlyList<string> paths = OpenFileSelection.Paths(answer);

        // Assert
        Assert.Equal(["C:\\Palettes\\Minato.impl"], paths);
    }

    [Fact]
    public void Paths_SeveralFiles_JoinsEachNameToTheFolder()
    {
        // Arrange
        string answer = "C:\\Palettes\0Gold.impl\0Minato Blue.impl\0\0";

        // Act
        IReadOnlyList<string> paths = OpenFileSelection.Paths(answer);

        // Assert
        Assert.Equal(["C:\\Palettes\\Gold.impl", "C:\\Palettes\\Minato Blue.impl"], paths);
    }

    [Fact]
    public void Paths_NothingPicked_ReturnsNoPaths()
    {
        // Act
        IReadOnlyList<string> paths = OpenFileSelection.Paths("\0\0");

        // Assert
        Assert.Empty(paths);
    }
}
