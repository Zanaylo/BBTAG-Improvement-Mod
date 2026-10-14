using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Compiling;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Compiling;

public class ScriptCompilerTests
{
    private const int SlotType = 2;
    private const int ConstantType = 0;

    [Fact]
    public void Compile_StateWithUpon_WritesTableAndCommands()
    {
        // Arrange
        string source = """
            @State
            def NmlAtk5A():

                def upon_On_Initialize():
                    Damage(1200)
                sprite('rg200_00', 3)
            """;
        byte[] expected = new ScriptBytes()
            .State("NmlAtk5A")
            .Command(15, 0).Command(ScriptTestDatabase.Damage, 1200).Command(16)
            .Command(2, "rg200_00", 3)
            .Command(1)
            .Build();

        // Act
        CompiledScript compiled = CreateScriptCompiler().Compile(source);

        // Assert
        Assert.Equal(expected, compiled.Bytes);
    }

    [Fact]
    public void Compile_IfWithElse_WritesBothBlocks()
    {
        // Arrange
        string source = """
            @Subroutine
            def Check():
                if SLOT_IsInOverdrive:
                    Damage(1)
                else:
                    Damage(2)
            """;
        byte[] expected = new ScriptBytes()
            .Subroutine("Check")
            .Command(4, SlotType, 51).Command(ScriptTestDatabase.Damage, 1).Command(5)
            .Command(56).Command(ScriptTestDatabase.Damage, 2).Command(57)
            .Command(9)
            .Build();

        // Act
        CompiledScript compiled = CreateScriptCompiler().Compile(source);

        // Assert
        Assert.Equal(expected, compiled.Bytes);
    }

    [Fact]
    public void Compile_AssignmentsOfEachKind_PicksStoreModifyOperationAndOperationInto()
    {
        // Arrange
        string source = """
            @Subroutine
            def Math():
                SLOT_3 = 5
                SLOT_3 = SLOT_3 + 1
                SLOT_0 = SLOT_3 == 2
                SLOT_4 = SLOT_3 * 2
            """;
        byte[] expected = new ScriptBytes()
            .Subroutine("Math")
            .Command(41, SlotType, 3, ConstantType, 5)
            .Command(49, 0, SlotType, 3, ConstantType, 1)
            .Command(40, 9, SlotType, 3, ConstantType, 2)
            .Command(47, 2, SlotType, 3, ConstantType, 2, SlotType, 4)
            .Command(9)
            .Build();

        // Act
        CompiledScript compiled = CreateScriptCompiler().Compile(source);

        // Assert
        Assert.Equal(expected, compiled.Bytes);
    }

    [Fact]
    public void Compile_ConditionWithComparison_WorksItOutIntoSlotZeroFirst()
    {
        // Arrange
        string source = """
            @Subroutine
            def Check():
                if not SLOT_3 > 4:
                    pass
            """;
        byte[] expected = new ScriptBytes()
            .Subroutine("Check")
            .Command(40, 10, SlotType, 3, ConstantType, 4)
            .Command(54, SlotType, 0).Command(55)
            .Command(9)
            .Build();

        // Act
        CompiledScript compiled = CreateScriptCompiler().Compile(source);

        // Assert
        Assert.Equal(expected, compiled.Bytes);
    }

    [Fact]
    public void Compile_NamedValuesAndFoldedNumber_WritesTheirNumbers()
    {
        // Arrange
        string source = """
            @Subroutine
            def Values():
                GroundedHitstunAnimation(AIR_VERTICAL)
                clearUponHandler(upon_Hit)
                CheckInput(INPUT_236)
                Damage(600 * 2)
            """;
        byte[] expected = new ScriptBytes()
            .Subroutine("Values")
            .Command(ScriptTestDatabase.HitAnimation, 10)
            .Command(ScriptTestDatabase.ClearUpon, 10)
            .Command(ScriptTestDatabase.CheckInput, 172)
            .Command(ScriptTestDatabase.Damage, 1200)
            .Command(9)
            .Build();

        // Act
        CompiledScript compiled = CreateScriptCompiler().Compile(source);

        // Assert
        Assert.Equal(expected, compiled.Bytes);
    }

    [Fact]
    public void Compile_RawState_WritesNoClosingCommand()
    {
        // Arrange
        string source = """
            @RawState
            def Tangled():
                upon(0)
                endIf()
            """;
        byte[] expected = new ScriptBytes().State("Tangled").Command(15, 0).Command(5).Build();

        // Act
        CompiledScript compiled = CreateScriptCompiler().Compile(source);

        // Assert
        Assert.Equal(expected, compiled.Bytes);
    }

    [Fact]
    public void Compile_StateNameInDecorator_UsesThatName()
    {
        // Arrange
        string source = """
            @State('Effect.01')
            def unnamed_0():
                pass
            """;
        byte[] expected = new ScriptBytes().State("Effect.01").Command(1).Build();

        // Act
        CompiledScript compiled = CreateScriptCompiler().Compile(source);

        // Assert
        Assert.Equal(expected, compiled.Bytes);
    }

    [Fact]
    public void Compile_Command_MapsItsBytesBackToItsLine()
    {
        // Arrange
        string source = """
            @State
            def NmlAtk5A():
                sprite('rg200_00', 3)
                Damage(1200)
            """;
        int damageOffset = ScriptBinary.DataStartFor(1) + 36 + 40;

        // Act
        CompiledScript compiled = CreateScriptCompiler().Compile(source);

        // Assert
        Assert.Equal(4, compiled.Lines.LineAt(damageOffset + 4));
    }

    [Fact]
    public void Compile_UnknownCommand_ThrowsScriptCompileExceptionWithItsLine()
    {
        // Arrange
        string source = """
            @State
            def NmlAtk5A():
                Damage(1200)
                Damag(1500)
            """;

        // Act
        Exception? exception = Record.Exception(() => CreateScriptCompiler().Compile(source));

        // Assert
        ScriptCompileException compileException = Assert.IsType<ScriptCompileException>(exception);
        Assert.Equal(4, compileException.Line);
    }

    [Fact]
    public void Compile_WrongNumberOfValues_ThrowsScriptCompileException()
    {
        // Arrange
        string source = """
            @State
            def NmlAtk5A():
                Damage(1200, 3)
            """;

        // Act
        Exception? exception = Record.Exception(() => CreateScriptCompiler().Compile(source));

        // Assert
        Assert.IsType<ScriptCompileException>(exception);
    }

    [Fact]
    public void Compile_FunctionWithoutDecorator_ThrowsScriptCompileException()
    {
        // Arrange
        string source = """
            def NmlAtk5A():
                pass
            """;

        // Act
        Exception? exception = Record.Exception(() => CreateScriptCompiler().Compile(source));

        // Assert
        Assert.IsType<ScriptCompileException>(exception);
    }

    public ScriptCompiler CreateScriptCompiler() => new(ScriptTestDatabase.Create());
}
