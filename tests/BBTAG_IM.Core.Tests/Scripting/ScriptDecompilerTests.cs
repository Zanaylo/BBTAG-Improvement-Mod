using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Compiling;
using BBTAG_IM.Core.Scripting.EditorHelp;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting;

public class ScriptDecompilerTests
{
    private const int SlotType = 2;
    private const int ConstantType = 0;
    private const string BlankLine = "\n\n";

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
        string expected = ScriptStubs.EditorSettings + BlankLine + """
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
        string expected = ScriptStubs.EditorSettings + BlankLine + """
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

    [Fact]
    public void Decompile_StateNameOfAStateInTheFile_WritesItWithoutQuotes()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("NmlAtk2B").Command(ScriptTestDatabase.SharedGatling, "NmlAtk5B").Command(1)
            .State("NmlAtk5B").Command(1)
            .Build();

        // Act
        string text = CreateScriptDecompiler().Decompile(Read(bytes));

        // Assert
        Assert.Contains("    SharedGatling(NmlAtk5B)\n", text);
    }

    [Fact]
    public void Decompile_StateNameThatIsNotInTheFile_KeepsTheQuotes()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("NmlAtk2B").Command(ScriptTestDatabase.SharedGatling, "CmnActStand").Command(1)
            .Build();

        // Act
        string text = CreateScriptDecompiler().Decompile(Read(bytes));

        // Assert
        Assert.Contains("    SharedGatling('CmnActStand')\n", text);
    }

    [Fact]
    public void Decompile_BBToolsStyle_KeepsStateNamesInQuotes()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("NmlAtk2B").Command(ScriptTestDatabase.SharedGatling, "NmlAtk5B").Command(1)
            .State("NmlAtk5B").Command(1)
            .Build();
        ScriptDecompiler decompiler = new(ScriptTestDatabase.Create(), ScriptStyle.BBTools);

        // Act
        string text = decompiler.Decompile(Read(bytes));

        // Assert
        Assert.Contains("    SharedGatling('NmlAtk5B')\n", text);
    }

    [Fact]
    public void Decompile_StateNamesWithoutQuotes_CompileBackToTheSameBytes()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("NmlAtk2B").Command(ScriptTestDatabase.SharedGatling, "5B").Command(1)
            .State("5B").Command(1)
            .Build();

        // Act
        string text = CreateScriptDecompiler().Decompile(Read(bytes));
        CompiledScript compiled = new ScriptCompiler(ScriptTestDatabase.Create()).Compile(text);

        // Assert
        Assert.Contains("SharedGatling(__5B)", text);
        Assert.Equal(bytes, compiled.Bytes);
    }

    [Fact]
    public void Decompile_ReadableStyle_StartsWithTheEditorSettings()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().State("NmlAtk5A").Command(1).Build();

        // Act
        string text = CreateScriptDecompiler().Decompile(Read(bytes));

        // Assert
        Assert.StartsWith(ScriptStubs.EditorSettings + "\n\n@State\n", text);
    }

    [Fact]
    public void Decompile_BBToolsStyle_HasNoEditorSettings()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().State("NmlAtk5A").Command(1).Build();
        ScriptDecompiler decompiler = new(ScriptTestDatabase.Create(), ScriptStyle.BBTools);

        // Act
        string text = decompiler.Decompile(Read(bytes));

        // Assert
        Assert.StartsWith("@State\n", text);
    }

    [Fact]
    public void Decompile_MangoStyleUponOpenedInsideAnother_NestsItLikeTheStack()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("ShieldBit")
            .Command(15, 0).Command(ScriptTestDatabase.Damage, 1)
            .Command(15, 10).Command(ScriptTestDatabase.Damage, 2).Command(16)
            .Command(16)
            .Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.StartsWith("@State\ndef ShieldBit():\n\n    def upon_IMMEDIATE():\n        Damage(1)\n\n        def upon_ON_HIT_OR_BLOCK():\n            Damage(2)\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleBlockClosedByAnotherCommand_EndsTheBlockWithThatCommand()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A")
            .Command(15, 0).Command(ScriptTestDatabase.Damage, 1).Command(5)
            .Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("    def upon_IMMEDIATE():\n        Damage(1)\n        endIf()\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleCloserWithNothingOpen_WritesItAsACall()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A")
            .Command(ScriptTestDatabase.Damage, 1).Command(16).Command(ScriptTestDatabase.Damage, 2)
            .Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("    Damage(1)\n    endUpon()\n    Damage(2)\n", text);
        Assert.DoesNotContain("@RawState", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleComparisonThenIf_WritesTheComparisonInTheIf()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A")
            .Command(40, (int)ScriptOperator.Greater, SlotType, 51, ConstantType, 25000)
            .Command(4, SlotType, 0).Command(ScriptTestDatabase.Damage, 1).Command(5)
            .Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("    if (SLOT_51 > 25000):\n        Damage(1)\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleIfNot_PutsTheNotInParentheses()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A")
            .Command(54, SlotType, 51).Command(ScriptTestDatabase.Damage, 1).Command(55)
            .Command(56).Command(ScriptTestDatabase.Damage, 2).Command(57)
            .Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("    if (not SLOT_51):\n        Damage(1)\n    else:\n        Damage(2)\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleElseWithoutAnIfBefore_WritesItAsACall()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A")
            .Command(ScriptTestDatabase.Damage, 1).Command(56).Command(ScriptTestDatabase.Damage, 2).Command(57)
            .Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("    Else()\n    Damage(2)\n    endElse()\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleBlockStillOpenWhenTheStateEnds_KeepsTheRawLayout()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A")
            .Command(15, 0).Command(ScriptTestDatabase.Damage, 1)
            .Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("@RawState", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleStateWithoutAnEnd_KeepsTheNestingUnderRawState()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A").Command(15, 0).Command(ScriptTestDatabase.Damage, 1).Command(16).Command(ScriptTestDatabase.Damage, 2)
            .State("B").Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("@RawState\ndef A():\n\n    def upon_IMMEDIATE():\n        Damage(1)\n    Damage(2)\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleEndInTheMiddle_WritesItAsACall()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A").Command(ScriptTestDatabase.Damage, 1).Command(1).Command(ScriptTestDatabase.Damage, 2).Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("@State\ndef A():\n    Damage(1)\n    endState()\n    Damage(2)\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleUponNumbers_AreWrittenLikeThe2019Parser()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A")
            .Command(15, 33).Command(ScriptTestDatabase.ClearUpon, 33).Command(16)
            .Command(15, 1).Command(16)
            .Command(15, 2).Command(16)
            .Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("    def upon_33():\n        clearUponHandler(33)\n", text);
        Assert.Contains("    def upon_STATE_END():\n", text);
        Assert.Contains("    def upon_LANDING():\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleStoreValue_UsesTheSlotNumber()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().State("A").Command(41, SlotType, 56, ConstantType, 1).Command(1).Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("    SLOT_56 = 1\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleCallThenIf_WritesTheCallInTheIf()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A")
            .Command(ScriptTestDatabase.CheckInput, 0xAC)
            .Command(4, SlotType, 0).Command(ScriptTestDatabase.Damage, 1).Command(5)
            .Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("    if CheckInput(0xac):\n        Damage(1)\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleMoveRegister_StaysAPlainCall()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A")
            .Command(14001, "NmlAtk5A", 0x0402).Command(14002)
            .Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("    Move_Register('NmlAtk5A', 0x402)\n    Move_EndRegister()\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyle_HasNoEditorSettings()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().State("A").Command(1).Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.StartsWith("@State\n", text);
    }

    [Fact]
    public void Decompile_MangoStyleEmptyState_WritesPass()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().State("A").Command(1).Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("def A():\n    pass\n", text);
        Assert.Equal(bytes, CompileMango(text));
    }

    [Fact]
    public void Decompile_MangoStyleStateReference_IsText()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("A").Command(ScriptTestDatabase.SharedGatling, "B").Command(1)
            .State("B").Command(1)
            .Build();

        // Act
        string text = Mango(bytes);

        // Assert
        Assert.Contains("SharedGatling('B')", text);
    }

    private static string Mango(byte[] bytes) => new ScriptDecompiler(ScriptTestDatabase.Create(), ScriptStyle.Mango).Decompile(Read(bytes));

    private static byte[] CompileMango(string text) => new ScriptCompiler(ScriptTestDatabase.Create()).Compile(text).Bytes;

    public ScriptDecompiler CreateScriptDecompiler() => new(ScriptTestDatabase.Create());

    private static ScriptBinary Read(byte[] bytes) => ScriptBinary.Read(bytes, ScriptTestDatabase.Create());
}
