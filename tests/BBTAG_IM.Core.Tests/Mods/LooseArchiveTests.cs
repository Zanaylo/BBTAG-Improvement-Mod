using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Mods;

public class LooseArchiveTests
{
    [Fact]
    public void Write_TwoFiles_GivesAnArchiveTheReaderUnderstands()
    {
        // Arrange
        ArchiveFile[] files = [new("scr_brg.bin", [1, 2, 3]), new("scr_brgea.bin", [4])];

        // Act
        byte[] archive = LooseArchive.Write(files);

        // Assert
        Assert.Equal(["scr_brg.bin", "scr_brgea.bin"], Read(archive).Select(file => file.Name));
        Assert.Equal([4], Read(archive)[1].Bytes);
    }

    [Fact]
    public void Write_FileData_StartsEveryFileOnSixteenBytes()
    {
        // Arrange
        ArchiveFile[] files = [new("a.bin", [1, 2, 3]), new("b.bin", [4])];

        // Act
        byte[] archive = LooseArchive.Write(files);

        // Assert
        int dataOffset = FpacArchive.DataOffset(archive);
        Assert.All(FpacArchive.Entries(archive.AsSpan(0, dataOffset)), entry => Assert.Equal(0, entry.Offset % 16));
    }

    [Fact]
    public void Merge_LooseFileWithTheSameName_ReplacesItAndKeepsTheOthers()
    {
        // Arrange
        byte[] original = SpriteFiles.Fpac(("scr_brg.bin", [1, 2]), ("scr_brgea.bin", [3]));
        ArchiveFile[] loose = [new("SCR_BRG.bin", [9, 9, 9])];

        // Act
        byte[] merged = LooseArchive.Merge(original, loose);

        // Assert
        Assert.Equal(
            [("scr_brg.bin", new byte[] { 9, 9, 9 }), ("scr_brgea.bin", new byte[] { 3 })],
            Read(merged).Select(file => (file.Name, file.Bytes)));
    }

    [Fact]
    public void Merge_NoOriginalArchive_UsesOnlyTheLooseFiles()
    {
        // Arrange
        ArchiveFile[] loose = [new("scr_rar.bin", [7])];

        // Act
        byte[] merged = LooseArchive.Merge(null, loose);

        // Assert
        Assert.Equal(["scr_rar.bin"], Read(merged).Select(file => file.Name));
    }

    [Fact]
    public void Write_NameLongerThanTheShortestField_WidensTheNameField()
    {
        // Arrange
        ArchiveFile[] files = [new("rg000_00ex_long_name.jonbin", [1])];

        // Act
        byte[] archive = LooseArchive.Write(files);

        // Assert
        Assert.Equal("rg000_00ex_long_name.jonbin", Read(archive).Single().Name);
    }

    private static List<ArchiveFile> Read(byte[] archive) => LooseArchive.Read(archive).ToList();
}
