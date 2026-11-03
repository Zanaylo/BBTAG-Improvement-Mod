using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class LoadedArchiveTests
{
    private const uint ArchiveAddress = 0x4D2F6900;
    private const uint BufferAddress = 0x01000000;

    private readonly FakeGameMemory _memory = new();
    private readonly byte[] _archive = SpriteFiles.Fpac(("rg000_00.jonbin", [1, 2]), ("rg000_01.jonbin", [3, 4, 5]));

    [Fact]
    public void Find_FileOfTheArchive_ReturnsWhereTheGameReadsIt()
    {
        // Arrange
        LoadedArchive archive = CreateLoadedArchive();

        // Act
        ArchiveEntry? entry = archive.Find("rg000_01.jonbin");

        // Assert
        Assert.NotNull(entry);
        Assert.Equal(ArchiveAddress + (uint)FpacDataOffset() + 2, entry.Address);
        Assert.Equal(3, entry.Size);
        Assert.Equal([3, 4, 5], _memory.BytesFrom(entry.Address, entry.Size));
    }

    [Fact]
    public void Find_NameInAnotherCase_FindsTheFile()
    {
        // Arrange
        LoadedArchive archive = CreateLoadedArchive();

        // Act
        ArchiveEntry? entry = archive.Find("RG000_00.JONBIN");

        // Assert
        Assert.Equal(2, entry?.Size);
    }

    [Fact]
    public void Find_FileNotInTheArchive_ReturnsNull()
    {
        // Arrange
        LoadedArchive archive = CreateLoadedArchive();

        // Act
        ArchiveEntry? entry = archive.Find("rg999_00.jonbin");

        // Assert
        Assert.Null(entry);
    }

    [Fact]
    public void Find_AddressWithoutAnArchive_ReturnsNull()
    {
        // Arrange
        _memory.Map(ArchiveAddress, 0x100);
        LoadedArchive archive = new(_memory, ArchiveAddress);

        // Act
        ArchiveEntry? entry = archive.Find("rg000_00.jonbin");

        // Assert
        Assert.Null(entry);
    }

    [Fact]
    public void Find_UnreadableAddress_ReturnsNull()
    {
        // Arrange
        LoadedArchive archive = new(_memory, ArchiveAddress);

        // Act
        ArchiveEntry? entry = archive.Find("rg000_00.jonbin");

        // Assert
        Assert.Null(entry);
    }

    [Fact]
    public void Point_BufferBelowTheArchive_MakesTheGameReadTheBuffer()
    {
        // Arrange
        LoadedArchive archive = CreateLoadedArchive();
        ArchiveEntry entry = archive.Find("rg000_01.jonbin")!;

        // Act
        archive.Point(entry, BufferAddress, 40);

        // Assert
        ArchiveEntry moved = archive.Find("rg000_01.jonbin")!;
        Assert.Equal(BufferAddress, moved.Address);
        Assert.Equal(40, moved.Size);
        Assert.Equal(BufferAddress, unchecked(ArchiveAddress + (uint)FpacDataOffset() + _memory.ReadUInt32(entry.OffsetField)));
    }

    private int FpacDataOffset() => FpacArchive.DataOffset(_archive);

    private LoadedArchive CreateLoadedArchive()
    {
        _memory.Map(ArchiveAddress, _archive.Length).BytesAt(ArchiveAddress, _archive);
        return new LoadedArchive(_memory, ArchiveAddress);
    }
}
