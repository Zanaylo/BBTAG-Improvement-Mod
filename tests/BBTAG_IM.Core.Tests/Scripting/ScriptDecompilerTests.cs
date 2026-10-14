using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Compiling;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting;

public class ScriptDecompilerTests
{
    private const int SlotType = 2;
    private const int ConstantType = 0;

    [Fact]
    public void Decompile_StateWithUponAndIf_WritesNestedBlocks()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("NmlAtk5A")
            .Command(15, 0).Command(ScriptTestDatabase.Damage, 1200).Command(16)
            .Command(4, SlotType, 51).Command(5)
            .Command(1)
            .Build();
        string expected = """
            @State
            def NmlAtk5A():

                def upon_On_Initialize():
                    Damage(1200)
                if SLOT_IsInOverdrive:
                    pass

            """;

        // Act
        string text = CreateScriptDecompiler().Decompile(Read(bytes));

        // Assert
        Assert.Equal(expected.ReplaceLineEndings("\n"), text);
    }

    [Fact]
    public void Decompile_ElseWithoutIf_WritesTheFunctionAsRawCalls()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("Odd")
            .Command(ScriptTestDatabase.Damage, 1).Command(56).Command(57)
            .Command(1)
            .Build();
        string expected = """
            @RawState
            def Odd():
                Damage(1)
                Else()
                endElse()
                endState()

            """;

        // Act
        string text = CreateScriptDecompiler().Decompile(Read(bytes));

        // Assert
        Assert.Equal(expected.ReplaceLineEndings("\n"), text);
    }

    [Fact]
    public void Decompile_OperationIntoTheLeftSlot_WritesACallSoItCompilesBack()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .Subroutine("Math")
            .Command(47, 0, SlotType, 3, ConstantType, 1, SlotType, 3)
            .Command(9)
            .Build();

        // Act
        string text = CreateScriptDecompiler().Decompile(Read(bytes));

        // Assert
        Assert.Contains("PrivateFunction(0, SLOT_3, 1, SLOT_3)", text);
    }

    [Fact]
    public void Decompile_ThenCompile_GivesBackTheSameBytes()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .Subroutine("PreInit")
            .Command(40, 9, SlotType, 3, ConstantType, 2)
            .Command(54, SlotType, 0).Command(ScriptTestDatabase.CheckInput, 0x1F).Command(55)
            .Command(56).Command(36, 2).Command(ScriptTestDatabase.HitAnimation, 10).Command(35).Command(57)
            .Command(9)
            .State("032rock")
            .Command(14001, "NmlAtk5A", (1 << 8) + 4).Command(14002)
            .Command(1)
            .Build();
        ScriptDatabase database = ScriptTestDatabase.Create();

        // Act
        string text = CreateScriptDecompiler().Decompile(Read(bytes));
        CompiledScript compiled = new ScriptCompiler(database).Compile(text);

        // Assert
        Assert.Equal(bytes, compiled.Bytes);
    }

    [Fact]
    public void Decompile_CommandOutsideOfAFunction_ThrowsInvalidScriptException()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().Command(ScriptTestDatabase.Damage, 1).Build();

        // Act
        Exception? exception = Record.Exception(() => CreateScriptDecompiler().Decompile(Read(bytes)));

        // Assert
        Assert.IsType<InvalidScriptException>(exception);
    }

    public ScriptDecompiler CreateScriptDecompiler() => new(ScriptTestDatabase.Create());

    private static ScriptBinary Read(byte[] bytes) => ScriptBinary.Read(bytes, ScriptTestDatabase.Create());
}
