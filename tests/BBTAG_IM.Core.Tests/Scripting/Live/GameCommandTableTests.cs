using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class GameCommandTableTests
{
    private const uint CodeStart = 0x00772DC8;
    private const uint Table = 0x00B70BF0;

    private static readonly byte[] SizeLookup =
    [
        0x33, 0xC9, 0xBE, 0x02, 0x00, 0x00, 0x00, 0x90, 0x8D, 0x04, 0x0E, 0x99, 0x2B, 0xC2, 0xD1, 0xF8,
        0x8B, 0x14, 0xC5, 0xF0, 0x0B, 0xB7, 0x00, 0x3B, 0xD7,
    ];

    [Fact]
    public void Locate_GameCode_ReturnsTheTableAndItsLength()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, SizeLookup)]);

        // Act
        GameCommandTable table = GameCommandTable.Locate(image);

        // Assert
        Assert.Equal(new GameCommandTable(Table, 3), table);
    }

    [Fact]
    public void Locate_CodeWithoutTheLookup_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, [0x33, 0xC0, 0xC3])]);

        // Act
        Exception? exception = Record.Exception(() => GameCommandTable.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void Read_TableWithEndMarker_ReturnsEachRealCommandSize()
    {
        // Arrange
        FakeGameMemory memory = new FakeGameMemory()
            .Map(Table, 24)
            .UInt32At(Table, 0).UInt32At(Table + 4, 36)
            .UInt32At(Table + 8, 9000).UInt32At(Table + 12, 8)
            .UInt32At(Table + 16, uint.MaxValue).UInt32At(Table + 20, 4);
        GameCommandTable table = new(Table, 3);

        // Act
        IReadOnlyDictionary<int, int> sizes = table.Read(memory);

        // Assert
        Assert.Equal(new Dictionary<int, int> { [0] = 36, [9000] = 8 }, sizes);
    }
}
