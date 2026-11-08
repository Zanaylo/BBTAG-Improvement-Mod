using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class ScriptMapTests
{
    private readonly IGameHeap _gameHeapMock = Substitute.For<IGameHeap>();
    private readonly ScriptMemory _scriptMemory = new();

    public ScriptMapTests()
    {
        _scriptMemory.Load(new ScriptBytes().State("A").Command(1).Build());
        _gameHeapMock.Allocate(ScriptMap.NodeLength).Returns(ScriptMemory.FreeNodes);
    }

    [Fact]
    public void Add_NewName_FindsItWithItsOffset()
    {
        // Arrange
        uint template = _scriptMemory.AddNode(ScriptMemory.StateMap, "CmnActStand", 0);
        ScriptMap scriptMap = CreateScriptMap();

        // Act
        scriptMap.Add("NmlAtkTaunt", 720, template, _gameHeapMock);

        // Assert
        Assert.Contains(new ScriptMapNode(ScriptMemory.FreeNodes, "NmlAtkTaunt", 720), scriptMap.Nodes());
    }

    [Fact]
    public void Add_NewName_CopiesTheNodeAndKeyTypesOfTheTemplate()
    {
        // Arrange
        uint template = _scriptMemory.AddNode(ScriptMemory.StateMap, "CmnActStand", 0);
        ScriptMap scriptMap = CreateScriptMap();

        // Act
        uint node = scriptMap.Add("NmlAtkTaunt", 720, template, _gameHeapMock);

        // Assert
        Assert.Equal(
            [ScriptMemory.NodeVtable, ScriptMemory.KeyVtable],
            [(uint)_scriptMemory.Memory.Int32From(node), (uint)_scriptMemory.Memory.Int32From(node + 0xC)]);
    }

    [Fact]
    public void Add_BucketWithNodes_AppendsAfterTheLastOneAndCountsIt()
    {
        // Arrange
        uint first = _scriptMemory.AddNode(ScriptMemory.StateMap, "NmlAtkTaunt", 0);
        uint bucket = _scriptMemory.Bucket(ScriptMemory.StateMap, "NmlAtkTaunt");
        ScriptMap scriptMap = CreateScriptMap();

        // Act
        uint node = scriptMap.Add("NmlAtkTaunt", 720, first, _gameHeapMock);

        // Assert
        Assert.Equal([first, node], _scriptMemory.BucketChain(ScriptMemory.StateMap, "NmlAtkTaunt"));
        Assert.Equal(
            [first, node, 2u],
            [(uint)_scriptMemory.Memory.Int32From(node + 8), (uint)_scriptMemory.Memory.Int32From(bucket + 8), (uint)_scriptMemory.Memory.Int32From(bucket + 0xC)]);
    }

    [Fact]
    public void Add_EmptyBucket_MakesTheNodeItsHeadAndTail()
    {
        // Arrange
        uint template = _scriptMemory.AddNode(ScriptMemory.SubroutineMap, "Sub", 0);
        uint bucket = _scriptMemory.Bucket(ScriptMemory.StateMap, "NmlAtkTaunt");
        ScriptMap scriptMap = CreateScriptMap();

        // Act
        uint node = scriptMap.Add("NmlAtkTaunt", 720, template, _gameHeapMock);

        // Assert
        Assert.Equal(
            [node, node, 0u, 0u],
            [(uint)_scriptMemory.Memory.Int32From(bucket + 4), (uint)_scriptMemory.Memory.Int32From(bucket + 8), (uint)_scriptMemory.Memory.Int32From(node + 4), (uint)_scriptMemory.Memory.Int32From(node + 8)]);
    }

    [Fact]
    public void Remove_MiddleNode_LinksItsNeighbours()
    {
        // Arrange
        uint first = _scriptMemory.AddNode(ScriptMemory.StateMap, "AA", 0);
        uint middle = _scriptMemory.AddNode(ScriptMemory.StateMap, "AAA", 36);
        uint last = _scriptMemory.AddNode(ScriptMemory.StateMap, "AAAA", 72);
        ScriptMap scriptMap = CreateScriptMap();

        // Act
        scriptMap.Remove(new ScriptMapNode(middle, "AAA", 36));

        // Assert
        Assert.Equal([first, last], _scriptMemory.BucketChain(ScriptMemory.StateMap, "AA"));
        Assert.Equal(first, (uint)_scriptMemory.Memory.Int32From(last + 8));
    }

    [Fact]
    public void Remove_OnlyNode_EmptiesTheBucket()
    {
        // Arrange
        uint node = _scriptMemory.AddNode(ScriptMemory.StateMap, "NmlAtkTaunt", 0);
        uint bucket = _scriptMemory.Bucket(ScriptMemory.StateMap, "NmlAtkTaunt");
        ScriptMap scriptMap = CreateScriptMap();

        // Act
        scriptMap.Remove(new ScriptMapNode(node, "NmlAtkTaunt", 0));

        // Assert
        Assert.Equal(
            [0u, 0u, 0u],
            [(uint)_scriptMemory.Memory.Int32From(bucket + 4), (uint)_scriptMemory.Memory.Int32From(bucket + 8), (uint)_scriptMemory.Memory.Int32From(bucket + 0xC)]);
    }

    [Fact]
    public void Remove_NodeNotInTheMap_LeavesTheBucket()
    {
        // Arrange
        uint node = _scriptMemory.AddNode(ScriptMemory.StateMap, "NmlAtkTaunt", 0);
        ScriptMap scriptMap = CreateScriptMap();

        // Act
        scriptMap.Remove(new ScriptMapNode(ScriptMemory.FreeNodes, "NmlAtkTaunt", 0));

        // Assert
        Assert.Equal([node], _scriptMemory.BucketChain(ScriptMemory.StateMap, "NmlAtkTaunt"));
    }

    public ScriptMap CreateScriptMap() => new(_scriptMemory.Memory, ScriptMemory.StateMap);
}
