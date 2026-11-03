using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes.Editing;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class SpriteDirectoriesTests
{
    [Fact]
    public void SearchOrder_TwiceSizeFolder_StartsWithItAndItsExtraArchives()
    {
        // Act
        IReadOnlyList<string> order = SpriteDirectories.SearchOrder(SpriteFolder.Char2x);

        // Assert
        Assert.Equal(["char2x", "char2x_add_ex", "charfhd", "charfhd_add_ex", "char"], order);
    }

    [Fact]
    public void SearchOrder_FullHdFolder_StartsWithItAndItsExtraArchives()
    {
        // Act
        IReadOnlyList<string> order = SpriteDirectories.SearchOrder(SpriteFolder.CharFhd);

        // Assert
        Assert.Equal(["charfhd", "charfhd_add_ex", "char2x", "char2x_add_ex", "char"], order);
    }

    [Fact]
    public void SearchOrder_DefaultFolder_StartsWithIt()
    {
        // Act
        IReadOnlyList<string> order = SpriteDirectories.SearchOrder(SpriteFolder.Char);

        // Assert
        Assert.Equal(["char", "char2x", "char2x_add_ex", "charfhd", "charfhd_add_ex"], order);
    }

    [Theory]
    [InlineData("char", 1f)]
    [InlineData("charfhd", 1.5f)]
    [InlineData("charfhd_add_ex", 1.5f)]
    [InlineData("char2x", 2f)]
    [InlineData("char2x_add_ex", 2f)]
    public void DefaultPixelsPerUnit_Directory_IsTheSizeItsPicturesAreStoredAt(string directory, float pixelsPerUnit)
    {
        // Act
        float scale = SpriteDirectories.DefaultPixelsPerUnit(directory);

        // Assert
        Assert.Equal(pixelsPerUnit, scale);
    }
}
