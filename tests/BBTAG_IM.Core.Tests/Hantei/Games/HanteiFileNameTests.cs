using BBTAG_IM.Core.Hantei.Games;

namespace BBTAG_IM.Core.Tests.Hantei.Games;

public class HanteiFileNameTests
{
    [Theory]
    [InlineData("chr000.ha6", "data/chr000/chr000.ha6")]
    [InlineData("../BaseData.ha6", "data/BaseData.ha6")]
    [InlineData(@"..\BaseData.ha6", "data/BaseData.ha6")]
    public void InCharacter_ListedFile_LandsInTheRightFolder(string file, string expected)
    {
        // Act
        string name = HanteiFileName.InCharacter("chr000", file);

        // Assert
        Assert.Equal(expected, name);
    }

    [Fact]
    public void Normalize_WindowsPath_UsesForwardSlashes()
    {
        // Act
        string name = HanteiFileName.Normalize(@"data\chr000\chr000.cg");

        // Assert
        Assert.Equal("data/chr000/chr000.cg", name);
    }
}
