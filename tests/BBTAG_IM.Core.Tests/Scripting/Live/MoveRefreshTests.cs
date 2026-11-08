using System.Text;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class MoveRefreshTests
{
    private const uint Characters = 0x05000000;
    private const uint SubroutineBlock = 0x06000000;
    private const int Register = 14001;
    private const int EndRegister = 14002;

    private readonly ScriptMemory _scriptMemory = new();
    private readonly IScriptBuffers _scriptBuffersMock = Substitute.For<IScriptBuffers>();
    private readonly IGameHeap _gameHeapMock = Substitute.For<IGameHeap>();
    private readonly ISubroutineCall _subroutineCallMock = Substitute.For<ISubroutineCall>();
    private readonly List<string> _nodesDuringTheCall = [];

    public MoveRefreshTests()
    {
        _scriptMemory.Load(new ScriptBytes().State("A").Command(1).Build());
        _scriptMemory.AddNode(ScriptMemory.StateMap, "A", 0);
        _scriptMemory.Memory.Map(Characters, 16).UInt32At(Characters, MoveListTests.Player).Map(MoveListTests.Player, 0x22000)
            .UInt32At(MoveListTests.Player + 0x1400, ScriptMemory.Record);
        _scriptBuffersMock.Allocate(Arg.Any<byte[]>()).Returns(SubroutineBlock);
        _gameHeapMock.Allocate(ScriptMap.NodeLength).Returns(ScriptMemory.FreeNodes);
        _subroutineCallMock.When(call => call.Call(MoveListTests.Player, MoveRefresh.TemporarySubroutine)).Do(_ =>
        {
            ScriptRecord record = new(_scriptMemory.Memory, ScriptMemory.Record);
            _nodesDuringTheCall.AddRange(record.Nodes(record.SubroutineMap).Select(node => node.Name));
            MoveListTests.Register(_scriptMemory.Memory, "NmlAtk6E", "NmlAtk5A");
        });
    }

    [Fact]
    public void Refresh_RegistrationsChanged_PutsTheNewMovesWhereTheOldOnesWere()
    {
        // Arrange
        MoveListTests.Register(_scriptMemory.Memory, "Neutral", "NmlAtk5A", "FDash");
        MoveRefresh refresh = CreateMoveRefresh();

        // Act
        MoveRefreshResult result = refresh.Refresh(ScriptMemory.Record, MatchInit("NmlAtk5A"), MatchInit("NmlAtk6E", "NmlAtk5A"));

        // Assert
        MoveList moveList = new(_scriptMemory.Memory, MoveListTests.Player);
        Assert.Equal(MoveRefreshResult.Refreshed, result);
        Assert.Equal(["Neutral", "NmlAtk6E", "NmlAtk5A", "FDash"], Enumerable.Range(0, moveList.Count).Select(moveList.NameAt));
    }

    [Fact]
    public void Refresh_RegistrationsChanged_RunsThemFromATemporarySubroutineAndTakesItAwayAfter()
    {
        // Arrange
        MoveListTests.Register(_scriptMemory.Memory, "Neutral", "NmlAtk5A", "FDash");
        MoveRefresh refresh = CreateMoveRefresh();

        // Act
        refresh.Refresh(ScriptMemory.Record, MatchInit("NmlAtk5A"), MatchInit("NmlAtk6E", "NmlAtk5A"));

        // Assert
        ScriptRecord record = new(_scriptMemory.Memory, ScriptMemory.Record);
        Assert.Equal([MoveRefresh.TemporarySubroutine], _nodesDuringTheCall);
        Assert.Empty(record.Nodes(record.SubroutineMap));
        _scriptBuffersMock.Received(1).Allocate(Arg.Is<byte[]>(bytes => Encoding.Latin1.GetString(bytes, 4, MoveRefresh.TemporarySubroutine.Length) == MoveRefresh.TemporarySubroutine));
    }

    [Fact]
    public void Refresh_RegistrationsChanged_IndexesTheNamesAgain()
    {
        // Arrange
        MoveListTests.Register(_scriptMemory.Memory, "Neutral", "NmlAtk5A", "FDash");
        MoveRefresh refresh = CreateMoveRefresh();

        // Act
        refresh.Refresh(ScriptMemory.Record, MatchInit("NmlAtk5A"), MatchInit("NmlAtk6E", "NmlAtk5A"));

        // Assert
        Assert.Equal(4, _scriptMemory.Memory.Int32From(MoveListTests.NameCount));
        Assert.Equal("NmlAtk6E", Encoding.Latin1.GetString(_scriptMemory.Memory.BytesFrom(MoveListTests.Names + 32, 8)));
    }

    [Fact]
    public void Refresh_SameRegistrations_LeavesTheMoves()
    {
        // Arrange
        MoveListTests.Register(_scriptMemory.Memory, "Neutral", "NmlAtk5A", "FDash");
        MoveRefresh refresh = CreateMoveRefresh();

        // Act
        MoveRefreshResult result = refresh.Refresh(ScriptMemory.Record, MatchInit("NmlAtk5A"), MatchInit("NmlAtk5A"));

        // Assert
        Assert.Equal(MoveRefreshResult.Unchanged, result);
        _subroutineCallMock.DidNotReceiveWithAnyArgs().Call(default, default!);
    }

    [Fact]
    public void Refresh_OldMovesNotFoundInTheList_LeavesTheMovesForTheNextMatch()
    {
        // Arrange
        MoveListTests.Register(_scriptMemory.Memory, "Neutral", "FDash");
        MoveRefresh refresh = CreateMoveRefresh();

        // Act
        MoveRefreshResult result = refresh.Refresh(ScriptMemory.Record, MatchInit("NmlAtk5A"), MatchInit("NmlAtk6E", "NmlAtk5A"));

        // Assert
        Assert.Equal(MoveRefreshResult.NotRefreshed, result);
        _subroutineCallMock.DidNotReceiveWithAnyArgs().Call(default, default!);
    }

    [Fact]
    public void Refresh_MoreMovesThanTheGameHolds_LeavesTheMovesForTheNextMatch()
    {
        // Arrange
        MoveListTests.Register(_scriptMemory.Memory, [.. Enumerable.Range(0, MoveList.MostMoves - 1).Select(index => $"Move{index}"), "NmlAtk5A"]);
        MoveRefresh refresh = CreateMoveRefresh();

        // Act
        MoveRefreshResult result = refresh.Refresh(ScriptMemory.Record, MatchInit("NmlAtk5A"), MatchInit("NmlAtk6E", "NmlAtk5A"));

        // Assert
        Assert.Equal(MoveRefreshResult.NotRefreshed, result);
        _subroutineCallMock.DidNotReceiveWithAnyArgs().Call(default, default!);
    }

    [Fact]
    public void Refresh_CharacterRunningAnotherScript_LeavesItsMoves()
    {
        // Arrange
        MoveListTests.Register(_scriptMemory.Memory, "Neutral", "NmlAtk5A", "FDash");
        _scriptMemory.Memory.UInt32At(MoveListTests.Player + 0x1400, ScriptMemory.Record + ScriptRecord.Length);
        MoveRefresh refresh = CreateMoveRefresh();

        // Act
        MoveRefreshResult result = refresh.Refresh(ScriptMemory.Record, MatchInit("NmlAtk5A"), MatchInit("NmlAtk6E", "NmlAtk5A"));

        // Assert
        Assert.Equal(MoveRefreshResult.NotRefreshed, result);
        _subroutineCallMock.DidNotReceiveWithAnyArgs().Call(default, default!);
    }

    public MoveRefresh CreateMoveRefresh() =>
        new(new GameView(_scriptMemory.Memory, new GameAddresses(0, 0, Characters, 0)), _scriptBuffersMock, _gameHeapMock, _subroutineCallMock);

    private static ScriptBinary MatchInit(params string[] moves)
    {
        ScriptBytes bytes = new ScriptBytes().State("A").Command(1).Subroutine("MatchInit");

        foreach (string move in moves)
            bytes.Command(Register, move, 0x16).Command(EndRegister);

        return ScriptBinary.Read(bytes.Command(9).Build(), ScriptTestDatabase.Create());
    }
}
