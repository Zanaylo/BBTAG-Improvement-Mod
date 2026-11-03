using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class CommandListTests
{
    private const uint Table = 0x00B70BF0;

    private readonly FakeGameMemory _memoryMock = new();

    public CommandList CreateCommandList(int count = 3) => new(Table, count);

    [Fact]
    public void ReadSizes_TableWithEndMarker_ReturnsEachRealCommandSize()
    {
        // Arrange
        _memoryMock
            .Map(Table, 24)
            .UInt32At(Table, 0).UInt32At(Table + 4, 36)
            .UInt32At(Table + 8, 9000).UInt32At(Table + 12, 8)
            .UInt32At(Table + 16, uint.MaxValue).UInt32At(Table + 20, 4);
        CommandList list = CreateCommandList();

        // Act
        IReadOnlyDictionary<int, int> sizes = list.ReadSizes(_memoryMock);

        // Assert
        Assert.Equal(new Dictionary<int, int> { [0] = 36, [9000] = 8 }, sizes);
    }

    [Fact]
    public void ReadSizes_CommandsAddedAfterTheEndMarker_ReturnsThemToo()
    {
        // Arrange
        _memoryMock
            .Map(Table, 24)
            .UInt32At(Table, 9000).UInt32At(Table + 4, 8)
            .UInt32At(Table + 8, uint.MaxValue).UInt32At(Table + 12, uint.MaxValue)
            .UInt32At(Table + 16, 40019).UInt32At(Table + 20, 8);
        CommandList list = CreateCommandList();

        // Act
        IReadOnlyDictionary<int, int> sizes = list.ReadSizes(_memoryMock);

        // Assert
        Assert.Equal(new Dictionary<int, int> { [9000] = 8, [40019] = 8 }, sizes);
    }

    [Fact]
    public void ReadSizes_UnreadableTable_ThrowsInvalidScriptException()
    {
        // Arrange
        CommandList list = CreateCommandList();

        // Act
        Exception? exception = Record.Exception(() => list.ReadSizes(_memoryMock));

        // Assert
        Assert.IsType<InvalidScriptException>(exception);
    }

    [Fact]
    public void ReadSizes_NoCommands_ThrowsInvalidScriptException()
    {
        // Arrange
        _memoryMock.Map(Table, 24);
        CommandList list = CreateCommandList(count: 0);

        // Act
        Exception? exception = Record.Exception(() => list.ReadSizes(_memoryMock));

        // Assert
        Assert.IsType<InvalidScriptException>(exception);
    }
}
