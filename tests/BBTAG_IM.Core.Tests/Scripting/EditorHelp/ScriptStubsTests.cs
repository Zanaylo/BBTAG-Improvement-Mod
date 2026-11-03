using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.EditorHelp;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.EditorHelp;

public class ScriptStubsTests
{
    private const int SoulEater = 12045;

    private readonly ScriptDocs _docs = new(
        new Dictionary<int, CommandDoc> { [ScriptTestDatabase.Damage] = new("Sets the damage.", "(x)", "Multiplied by \"proration\".") },
        new Dictionary<int, string> { [51] = "Is in Overdrive." },
        new Dictionary<int, string> { [10] = "Attack hitting the opponent" });

    [Fact]
    public void Write_DocumentedCommand_WritesItsSignatureAndDocs()
    {
        // Act
        string stub = ScriptStubs.Write(CreateDatabase(), _docs);

        // Assert
        Assert.Contains("def Damage(value: int, /) -> None:\n", stub);
        Assert.Contains("Sets the damage.", stub);
        Assert.Contains("Multiplied by \\\"proration\\\".", stub);
        Assert.Contains("Command 9000", stub);
    }

    [Fact]
    public void Write_CommandThatAnswersInSlotZero_ReturnsInt()
    {
        // Act
        string stub = ScriptStubs.Write(CreateDatabase(), _docs);

        // Assert
        Assert.Contains("def CheckInput(input: int, /) -> int:\n", stub);
    }

    [Fact]
    public void Write_CommandWithSlotArguments_HasThePairedAndTheRawForm()
    {
        // Act
        string stub = ScriptStubs.Write(CreateDatabase(), _docs);

        // Assert
        Assert.Contains("@overload\ndef If(value: int, /) -> None:\n", stub);
        Assert.Contains("@overload\ndef If(value1: int, value2: int, /) -> None:\n", stub);
    }

    [Fact]
    public void Write_CommandThatTakesAState_AcceptsTheFunction()
    {
        // Act
        string stub = ScriptStubs.Write(CreateDatabase(), _docs);

        // Assert
        Assert.Contains("def SharedGatling(state: StateName, /) -> None:\n", stub);
        Assert.Contains("StateName = str | Callable[..., Any]\n", stub);
    }

    [Fact]
    public void Write_NamedCommand_AlsoHasItsNumberName()
    {
        // Act
        string stub = ScriptStubs.Write(CreateDatabase(), _docs);

        // Assert
        Assert.Contains("Unknown9000 = Damage\n", stub);
    }

    [Fact]
    public void Write_CommandWithoutAName_IsWrittenByNumber()
    {
        // Act
        string stub = ScriptStubs.Write(CreateDatabase(), _docs);

        // Assert
        Assert.Contains("def Unknown23055(", stub);
    }

    [Fact]
    public void Write_NamedValues_AreDocumentedConstants()
    {
        // Act
        string stub = ScriptStubs.Write(CreateDatabase(), _docs);

        // Assert
        Assert.Contains("INPUT_5A: int\n\"\"\"0x7 (7): 5A on the ground.\"\"\"\n", stub);
        Assert.Contains("MOVE_GROUND: int\n", stub);
        Assert.Contains("AIR_VERTICAL: int\n", stub);
        Assert.Contains("upon_Hit: int\n\"\"\"Upon 10: Attack hitting the opponent\"\"\"\n", stub);
    }

    [Fact]
    public void Write_Slots_AreDocumentedByNameAndNumber()
    {
        // Act
        string stub = ScriptStubs.Write(CreateDatabase(), _docs);

        // Assert
        Assert.Contains("SLOT_IsInOverdrive: int\n\"\"\"SLOT 51: Is in Overdrive.\"\"\"\n", stub);
        Assert.Contains("SLOT_51: int\n", stub);
        Assert.Contains("SLOT_255: int\n", stub);
    }

    [Fact]
    public void Write_InputNumbers_HaveTheirNumberNames()
    {
        // Act
        string stub = ScriptStubs.Write(CreateDatabase(), _docs);

        // Assert
        Assert.Contains("INPUT_0x0: int\n", stub);
        Assert.Contains("INPUT_0x1ff: int\n", stub);
    }

    [Fact]
    public void Write_Decorators_AreDefined()
    {
        // Act
        string stub = ScriptStubs.Write(CreateDatabase(), _docs);

        // Assert
        Assert.Contains("def State(*arguments: Any) -> Any:\n", stub);
        Assert.Contains("def StateRegister(*arguments: Any) -> Any:\n", stub);
        Assert.Contains("def RawSubroutine(*arguments: Any) -> Any:\n", stub);
    }

    [Fact]
    public void Write_UponArgument_AcceptsTheUponFunctionOfTheState()
    {
        // Arrange
        ScriptDatabase database = CreateDatabase();

        // Act
        string stub = ScriptStubs.Write(database, _docs);

        // Assert
        Assert.Contains("def clearUponHandler(upon: UponName, /) -> None:\n", stub);
        Assert.Contains("UponName = int | Callable[..., Any]\n", stub);
    }

    private static ScriptDatabase CreateDatabase() =>
        ScriptTestDatabase.Create()
            .WithGameSizes(new Dictionary<int, int> { [ScriptTestDatabase.Damage] = 8, [4] = 12, [43] = 8, [ScriptTestDatabase.SharedGatling] = 36, [23055] = 4, [ScriptTestDatabase.ClearUpon] = 8, [SoulEater] = 68 })
            .WithTagNames();
}
