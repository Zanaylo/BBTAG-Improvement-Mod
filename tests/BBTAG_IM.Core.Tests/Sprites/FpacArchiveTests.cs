using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Sprites;

public class FpacArchiveTests
{
    [Fact]
    public void DataOffset_ArchivePrefix_ReturnsWhereTheFilesStart()
    {
        // Arrange
        byte[] archive = SpriteFiles.Fpac(("rg000_00.hip", [1, 2]), ("rg000_01.hip", [3]));

        // Act
        int dataOffset = FpacArchive.DataOffset(archive.AsSpan(0, FpacArchive.PrefixLength));

        // Assert
        Assert.Equal(SpriteFiles.FpacPrefixLength + 2 * SpriteFiles.FpacEntryLength, dataOffset);
    }

    [Fact]
    public void DataOffset_FileWithoutTheArchiveSignature_ThrowsInvalidSpriteDataException()
    {
        // Arrange
        byte[] encrypted = new byte[FpacArchive.PrefixLength];
        encrypted[0] = 0x7A;

        // Act
        Exception? exception = Record.Exception(() => FpacArchive.DataOffset(encrypted));

        // Assert
        Assert.IsType<InvalidSpriteDataException>(exception);
    }

    [Fact]
    public void Entries_ArchiveHeader_ReturnsEachFileWithItsAbsoluteOffset()
    {
        // Arrange
        byte[] archive = SpriteFiles.Fpac(("rg000_00.hip", [1, 2]), ("rg000_01.hip", [3]));
        int dataOffset = FpacArchive.DataOffset(archive);

        // Act
        IReadOnlyList<FpacEntry> entries = FpacArchive.Entries(archive.AsSpan(0, dataOffset));

        // Assert
        Assert.Equal([new FpacEntry("rg000_00.hip", dataOffset, 2), new FpacEntry("rg000_01.hip", dataOffset + 2, 1)], entries);
    }

    [Fact]
    public void Entries_HeaderCutShort_ThrowsInvalidSpriteDataException()
    {
        // Arrange
        byte[] archive = SpriteFiles.Fpac(("rg000_00.hip", [1, 2]), ("rg000_01.hip", [3]));

        // Act
        Exception? exception = Record.Exception(() => FpacArchive.Entries(archive.AsSpan(0, FpacArchive.PrefixLength + 4)));

        // Assert
        Assert.IsType<InvalidSpriteDataException>(exception);
    }
}
