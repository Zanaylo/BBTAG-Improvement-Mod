using System.Text;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Game;

public class StageTableTests
{
    private const uint CodeAddress = 0x401000;
    private const int CodeSize = 0x200;
    private const uint GetterOffset = 0x100;
    private const uint Getter = CodeAddress + GetterOffset;
    private const uint Table = 0x00E5DAD0;
    private const uint RecordLength = 0xC4;
    private const int Boss = 16;
    private const int Takemikazuchi = 119;

    private readonly FakeGameMemory _memoryMock = new();

    public StageTable CreateStageTable(uint? getter = Getter) => new(_memoryMock, getter);

    [Fact]
    public void LocateGetter_CodeThatLoadsTheMatchStageName_ReturnsTheGetter()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeAddress, CreateStageCode().ToArray())]);

        // Act
        uint getter = StageTable.LocateGetter(image);

        // Assert
        Assert.Equal(Getter, getter);
    }

    [Fact]
    public void LocateGetter_CodeWithoutTheStageNameCall_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeAddress, [0x33, 0xC0, 0xC3])]);

        // Act
        Exception? exception = Record.Exception(() => StageTable.LocateGetter(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void IndexOf_StageAddedPastTheStockStages_ReturnsItsIndex()
    {
        // Arrange
        TableWithStages(Takemikazuchi + 1);
        StageAt(Boss, "boss");
        StageAt(Takemikazuchi, "takemikazuchi");
        StageTable stages = CreateStageTable();

        // Act
        int? index = stages.IndexOf("takemikazuchi");

        // Assert
        Assert.Equal(Takemikazuchi, index);
    }

    [Fact]
    public void IndexOf_NameThatOnlyStartsAnotherStage_SkipsThatStage()
    {
        // Arrange
        TableWithStages(Takemikazuchi + 1);
        StageAt(Boss, "boss_2");
        StageAt(Takemikazuchi, "boss");
        StageTable stages = CreateStageTable();

        // Act
        int? index = stages.IndexOf("boss");

        // Assert
        Assert.Equal(Takemikazuchi, index);
    }

    [Fact]
    public void IndexOf_StageMissingFromTheTable_ReturnsNull()
    {
        // Arrange
        TableWithStages(Takemikazuchi);
        StageAt(Boss, "boss");
        StageTable stages = CreateStageTable();

        // Act
        int? index = stages.IndexOf("takemikazuchi");

        // Assert
        Assert.Null(index);
    }

    [Fact]
    public void IndexOf_TableNotFound_ReturnsNull()
    {
        // Arrange
        StageTable stages = CreateStageTable(getter: null);

        // Act
        int? index = stages.IndexOf("boss");

        // Assert
        Assert.Null(index);
    }

    [Fact]
    public void NameOf_StageInTheTable_ReturnsItsName()
    {
        // Arrange
        TableWithStages(Takemikazuchi + 1);
        StageAt(Takemikazuchi, "takemikazuchi");
        StageTable stages = CreateStageTable();

        // Act
        string? name = stages.NameOf(Takemikazuchi);

        // Assert
        Assert.Equal("takemikazuchi", name);
    }

    [Fact]
    public void NameOf_StagePastTheReadableTable_ReturnsNull()
    {
        // Arrange
        TableWithStages(Boss);
        StageTable stages = CreateStageTable();

        // Act
        string? name = stages.NameOf(Takemikazuchi);

        // Assert
        Assert.Null(name);
    }

    [Fact]
    public void NameOf_NegativeStage_ReturnsNull()
    {
        // Arrange
        TableWithStages(Boss);
        StageTable stages = CreateStageTable();

        // Act
        string? name = stages.NameOf(-1);

        // Assert
        Assert.Null(name);
    }

    private void TableWithStages(int count)
    {
        _memoryMock.Map(Getter, 8).BytesAt(Getter, [0xB8, 0, 0, 0, 0, GameCode.Return]).UInt32At(Getter + 1, Table);
        _memoryMock.Map(Table, count * (int)RecordLength);
    }

    private void StageAt(int index, string name) =>
        _memoryMock.BytesAt(Table + (uint)index * RecordLength, Encoding.Latin1.GetBytes(name));

    private static GameCode CreateStageCode() =>
        new GameCode(CodeSize)
            .Bytes(0x000, "6A 01 FF B3 4C 28 00 00 6A 00 ?? ?? ?? ?? ??")
            .CallAt(0x00A, GetterOffset)
            .Bytes(GetterOffset, "B8 D0 DA E5 00 C3");
}
