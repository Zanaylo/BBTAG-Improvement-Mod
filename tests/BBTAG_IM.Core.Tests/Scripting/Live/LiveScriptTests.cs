using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Compiling;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class LiveScriptTests
{
    private const uint MovedFile = 0x06000000;

    private readonly IScriptBuffers _scriptBuffersMock = Substitute.For<IScriptBuffers>();
    private readonly ScriptMemory _scriptMemory = new();

    [Fact]
    public void Apply_ValueChanged_WritesItIntoTheLoadedScript()
    {
        // Arrange
        byte[] loaded = new ScriptBytes().State("A").Command(ScriptTestDatabase.Damage, 12).Command(1).Build();
        byte[] edited = new ScriptBytes().State("A").Command(ScriptTestDatabase.Damage, 15).Command(1).Build();
        LiveScript liveScript = CreateLiveScript(loaded);

        // Act
        ScriptApplyResult result = liveScript.Apply(Compiled(edited));

        // Assert
        Assert.Equal(ScriptApplyKind.InPlace, result.Kind);
        Assert.Equal(edited, _scriptMemory.Memory.BytesFrom(ScriptMemory.File, edited.Length));
    }

    [Fact]
    public void Apply_CommandAdded_MovesTheScriptAndRepointsTheMaps()
    {
        // Arrange
        byte[] loaded = new ScriptBytes().State("A").Command(1).State("B").Command(1).Build();
        byte[] edited = new ScriptBytes().State("A").Command(ScriptTestDatabase.Damage, 1).Command(1).State("B").Command(1).Build();
        _scriptMemory.AddNode(ScriptMemory.StateMap, "B", 40);
        _scriptBuffersMock.Allocate(Arg.Any<byte[]>()).Returns(MovedFile);
        LiveScript liveScript = CreateLiveScript(loaded);

        // Act
        ScriptApplyResult result = liveScript.Apply(Compiled(edited));

        // Assert
        ScriptRecord record = new(_scriptMemory.Memory, ScriptMemory.Record);
        Assert.Equal(ScriptApplyKind.Moved, result.Kind);
        Assert.Equal(MovedFile, record.File);
        Assert.Equal(48, record.Nodes(record.StateMap).Single().Offset);
    }

    [Fact]
    public void Apply_GameLoadedAnotherScriptIntoTheRecord_ThrowsScriptReloadedException()
    {
        // Arrange
        byte[] loaded = new ScriptBytes().State("A").Command(1).Build();
        LiveScript liveScript = CreateLiveScript(loaded);
        _scriptMemory.Memory.UInt32At(ScriptMemory.Record, 0x07000000);

        // Act
        Exception? exception = Record.Exception(() => liveScript.Apply(Compiled(loaded)));

        // Assert
        Assert.IsType<ScriptReloadedException>(exception);
    }

    [Fact]
    public void LineOf_InstructionInTheAppliedScript_ReturnsItsSourceLine()
    {
        // Arrange
        byte[] loaded = new ScriptBytes().State("A").Command(ScriptTestDatabase.Damage, 12).Command(1).Build();
        string source = """
            @State
            def A():
                Damage(15)
            """;
        CompiledScript compiled = new ScriptCompiler(ScriptTestDatabase.Create()).Compile(source);
        LiveScript liveScript = CreateLiveScript(loaded);
        liveScript.Apply(compiled);
        uint damage = ScriptMemory.File + (uint)ScriptBinary.DataStartFor(1) + 36;

        // Act
        int? line = liveScript.LineOf(damage);

        // Assert
        Assert.Equal(3, line);
    }

    [Fact]
    public void LineOf_InstructionOutsideTheScript_ReturnsNull()
    {
        // Arrange
        byte[] loaded = new ScriptBytes().State("A").Command(1).Build();
        LiveScript liveScript = CreateLiveScript(loaded);

        // Act
        int? line = liveScript.LineOf(0x09000000);

        // Assert
        Assert.Null(line);
    }

    public LiveScript CreateLiveScript(byte[] loaded)
    {
        _scriptMemory.Load(loaded);
        return new LiveScript(_scriptMemory.Memory, _scriptBuffersMock, ScriptMemory.Record, ScriptBinary.Read(loaded, ScriptTestDatabase.Create()), ScriptTestDatabase.Create());
    }

    private static CompiledScript Compiled(byte[] bytes) => new(bytes, new ScriptLineMap([]));
}
