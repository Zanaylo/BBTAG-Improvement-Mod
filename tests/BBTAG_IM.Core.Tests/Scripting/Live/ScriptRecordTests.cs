using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class ScriptRecordTests
{
    private readonly ScriptMemory _scriptMemory = new();

    [Fact]
    public void Nodes_MapWithEntriesInTwoBuckets_ReturnsEveryEntry()
    {
        // Arrange
        ScriptMemory script = _scriptMemory.Load(new ScriptBytes().State("A").Command(1).Build());
        uint first = script.AddNode(ScriptMemory.StateMap, "CmnActStand", 0);
        uint second = script.AddNode(ScriptMemory.StateMap, "NmlAtk5A", 40);
        ScriptRecord record = CreateScriptRecord();

        // Act
        IReadOnlyList<ScriptMapNode> nodes = record.Nodes(record.StateMap);

        // Assert
        Assert.Equal(
            [new ScriptMapNode(second, "NmlAtk5A", 40), new ScriptMapNode(first, "CmnActStand", 0)],
            nodes.OrderBy(node => node.Name, StringComparer.Ordinal).Reverse());
    }

    [Fact]
    public void Point_NewFile_WritesFileBytecodeCountAndTable()
    {
        // Arrange
        ScriptMemory script = _scriptMemory.Load(new ScriptBytes().State("A").Command(1).Build());
        ScriptRecord record = CreateScriptRecord();
        uint moved = 0x05000000;

        // Act
        record.Point(moved, stateCount: 3);

        // Assert
        Assert.Equal(
            [moved, moved + 4 + 3 * 36, 3u, moved + 4],
            [record.File, record.Bytecode, (uint)record.StateCount, record.StateTable]);
    }

    [Fact]
    public void SetOffset_Node_ChangesWhereTheFunctionStarts()
    {
        // Arrange
        ScriptMemory script = _scriptMemory.Load(new ScriptBytes().State("A").Command(1).Build());
        uint node = script.AddNode(ScriptMemory.StateMap, "A", 0);
        ScriptRecord record = CreateScriptRecord();

        // Act
        record.SetOffset(new ScriptMapNode(node, "A", 0), 96);

        // Assert
        Assert.Equal(96, record.Nodes(record.StateMap).Single().Offset);
    }

    public ScriptRecord CreateScriptRecord() => new(_scriptMemory.Memory, ScriptMemory.Record);
}
