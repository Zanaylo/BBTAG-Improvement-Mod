using BBTAG_IM.Core.Online;

namespace BBTAG_IM.Core.Tests.Online;

public class ByteSearchTests
{
    [Fact]
    public void Offsets_NeedleMissing_IsEmpty()
    {
        // Arrange
        byte[] haystack = [1, 2, 3, 4];

        // Act
        IReadOnlyList<int> offsets = ByteSearch.Offsets(haystack, [9]);

        // Assert
        Assert.Empty(offsets);
    }

    [Fact]
    public void Offsets_NeedleTwice_GivesBothOffsets()
    {
        // Arrange
        byte[] haystack = [7, 8, 0, 7, 8];

        // Act
        IReadOnlyList<int> offsets = ByteSearch.Offsets(haystack, [7, 8]);

        // Assert
        Assert.Equal([0, 3], offsets);
    }

    [Fact]
    public void Offsets_OverlappingNeedles_GivesEachStart()
    {
        // Arrange
        byte[] haystack = [5, 5, 5];

        // Act
        IReadOnlyList<int> offsets = ByteSearch.Offsets(haystack, [5, 5]);

        // Assert
        Assert.Equal([0, 1], offsets);
    }
}
