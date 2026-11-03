using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class GameCommandTableTests
{
    private const uint CodeStart = 0x00772DC8;
    private const uint Table = 0x00B70BF0;
    private const uint ExtendedTable = 0x05A10000;
    private const uint LastIndexOperand = CodeStart + 3;
    private const uint TableOperand = CodeStart + 19;

    private static readonly byte[] SizeLookup =
    [
        0x33, 0xC9, 0xBE, 0x02, 0x00, 0x00, 0x00, 0x90, 0x8D, 0x04, 0x0E, 0x99, 0x2B, 0xC2, 0xD1, 0xF8,
        0x8B, 0x14, 0xC5, 0xF0, 0x0B, 0xB7, 0x00, 0x3B, 0xD7,
    ];

    private readonly FakeGameMemory _memoryMock = new FakeGameMemory().Map(CodeStart, SizeLookup.Length).BytesAt(CodeStart, SizeLookup);

    public GameCommandTable CreateGameCommandTable() => new(CodeStart);

    [Fact]
    public void Locate_GameCode_ReturnsTheSizeLookup()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, SizeLookup)]);

        // Act
        GameCommandTable table = GameCommandTable.Locate(image);

        // Assert
        Assert.Equal(CreateGameCommandTable(), table);
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
    public void Current_StockCode_ReturnsTheListTheCodeSearches()
    {
        // Arrange
        GameCommandTable table = CreateGameCommandTable();

        // Act
        CommandList list = table.Current(_memoryMock);

        // Assert
        Assert.Equal(new CommandList(Table, 3), list);
    }

    [Fact]
    public void Current_CodePointedAtALongerListAfterStartup_ReturnsTheNewList()
    {
        // Arrange
        GameCommandTable table = CreateGameCommandTable();
        _memoryMock.Int32At(LastIndexOperand, 1666).UInt32At(TableOperand, ExtendedTable);

        // Act
        CommandList list = table.Current(_memoryMock);

        // Assert
        Assert.Equal(new CommandList(ExtendedTable, 1667), list);
    }
}
