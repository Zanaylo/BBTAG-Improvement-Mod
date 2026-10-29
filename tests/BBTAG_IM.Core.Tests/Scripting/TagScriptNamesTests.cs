using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting;

public class TagScriptNamesTests
{
    private const int MoveInputCommand = 14012;
    private const int NormalInputCommand = 14001;
    private const int SoulEaterCommand = 12045;

    [Theory]
    [InlineData(0x37, "INPUT_HOLD_1")]
    [InlineData(0x78, "INPUT_HOLD_6")]
    [InlineData(0x9F, "INPUT_HOLD_9")]
    [InlineData(0x79, "INPUT_HOLD_FORWARD")]
    [InlineData(0x5F, "INPUT_HOLD_BACK")]
    [InlineData(0x45, "INPUT_HOLD_DOWN")]
    [InlineData(0x93, "INPUT_HOLD_UP")]
    [InlineData(0x38, "INPUT_HOLD_DOWN_OR_BACK")]
    [InlineData(0x80, "INPUT_NOT_HOLD_6")]
    [InlineData(0x81, "INPUT_NOT_HOLD_FORWARD")]
    [InlineData(0x02, "INPUT_PRESS_NOW_A")]
    [InlineData(0x0E, "INPUT_NOT_HOLD_B")]
    [InlineData(0xDE, "INPUT_PRESS_BC")]
    [InlineData(0xE5, "INPUT_DASH_66")]
    public void MoveInputs_GameValue_HasAReadableName(int value, string expected)
    {
        // Act
        string? name = TagMoveInputs.Table.NameOf(value);

        // Assert
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(0x2000, "MOVE_GROUND")]
    [InlineData(0x2001, "MOVE_AIR")]
    [InlineData(0x3089, "MOVE_DISTORTION")]
    [InlineData(0x300A, "MOVE_UNLESS_SLOT_6")]
    [InlineData(0x300E, "MOVE_UNLESS_SLOT_10")]
    [InlineData(0x3000, "MOVE_IF_SLOT_4")]
    [InlineData(0x3007, "MOVE_IF_SLOT_11")]
    [InlineData(0x3025, "MOVE_IF_SLOT_4_IS_1")]
    [InlineData(0x307E, "MOVE_IF_SLOT_4_IS_20")]
    [InlineData(0x302E, "MOVE_GAUGE_0_SECTION")]
    [InlineData(0x3040, "MOVE_GAUGE_4_EMPTY")]
    [InlineData(0x3037, "MOVE_HEIGHT_100000")]
    [InlineData(0x3053, "MOVE_SKILL_GAUGE_STEP_1")]
    [InlineData(0x305B, "MOVE_IF_SLOT_12_BIT_0")]
    [InlineData(0x306A, "MOVE_IF_SLOT_12_BIT_15")]
    [InlineData(0x3086, "MOVE_EX")]
    [InlineData(0x3081, "MOVE_OD")]
    [InlineData(0x304A, "MOVE_ASTRAL")]
    [InlineData(0x3083, "MOVE_0x3083")]
    public void MoveConditions_GameValue_HasAReadableName(int value, string expected)
    {
        // Act
        string? name = TagMoveConditions.Table.NameOf(value);

        // Assert
        Assert.Equal(expected, name);
    }

    [Fact]
    public void Tables_EveryName_HasADescription()
    {
        // Act
        IEnumerable<NamedValue> named = TagMoveInputs.Table.Named.Concat(TagMoveConditions.Table.Named);

        // Assert
        Assert.All(named, value => Assert.False(string.IsNullOrWhiteSpace(TagMoveInputs.Table.DescriptionOf(value.Value) ?? TagMoveConditions.Table.DescriptionOf(value.Value))));
    }

    [Fact]
    public void Tables_NoDescription_Guesses()
    {
        // Act
        IEnumerable<string> descriptions = TagMoveInputs.Table.Named.Select(value => TagMoveInputs.Table.DescriptionOf(value.Value)!)
            .Concat(TagMoveConditions.Table.Named.Select(value => TagMoveConditions.Table.DescriptionOf(value.Value)!))
            .Concat(TagNormalInputs.Instance.Named.Select(value => value.Description));

        // Assert
        Assert.All(descriptions, description => Assert.DoesNotContain("probabl", description, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WithTagNames_NormalInput_UsesTheTagEncoding()
    {
        // Arrange
        ScriptDatabase database = ScriptTestDatabase.Create();

        // Act
        ScriptDatabase readable = database.WithTagNames();

        // Assert
        Assert.Equal("INPUT_5A", readable.Inputs.NameOf(NormalInputCommand, 0x07, hex: true));
    }

    [Fact]
    public void WithTagNames_MoveInput_KeepsTheBBToolsNamesAndAddsTheTagOnes()
    {
        // Arrange
        ScriptDatabase database = ScriptTestDatabase.Create();

        // Act
        ScriptDatabase readable = database.WithTagNames();

        // Assert
        Assert.Equal("INPUT_236", readable.Inputs.NameOf(MoveInputCommand, 172, hex: true));
        Assert.Equal("INPUT_HOLD_6", readable.Inputs.NameOf(MoveInputCommand, 0x78, hex: true));
    }

    [Fact]
    public void WithTagNames_MoveCondition_IsNamed()
    {
        // Arrange
        ScriptDatabase database = ScriptTestDatabase.Create();

        // Act
        ScriptDatabase readable = database.WithTagNames();

        // Assert
        Assert.Equal("MOVE_AIR", readable.MoveConditions.NameOf(0x2001));
    }

    [Fact]
    public void WithTagNames_UnnamedCommand_GetsItsTagName()
    {
        // Arrange
        ScriptDatabase database = ScriptTestDatabase.Create().WithGameSizes(new Dictionary<int, int> { [SoulEaterCommand] = 68 });

        // Act
        ScriptDatabase readable = database.WithTagNames();

        // Assert
        Assert.Equal("LifestealMultiplierValues", readable.Command(SoulEaterCommand).Name);
        Assert.Equal(SoulEaterCommand, readable.Find("Unknown12045")?.Id);
    }

    [Fact]
    public void WithTagNames_CommandThatAlreadyHasAName_KeepsIt()
    {
        // Arrange
        ScriptDatabase database = new(
            [new CommandDefinition(SoulEaterCommand, "SoulEater", CommandFormat.Parse("16i"), new HashSet<int>(), HexArguments.None)],
            NameTable.Empty, NameTable.Empty, NameTable.Empty, NameTable.Empty, InputNames.Empty);

        // Act
        ScriptDatabase readable = database.WithTagNames();

        // Assert
        Assert.Equal("SoulEater", readable.Command(SoulEaterCommand).Name);
    }
}
