using BBTAG_IM.Core.Tables;

namespace BBTAG_IM.Core.Tests.Tables;

public class CheatTableTests
{
    private const string Script = "Auto Assembler Script";
    private const string CoreDescription = "\"Core  / Geo Stuff\"";

    [Fact]
    public void Parse_ScriptsGroupsAndValues_KeepsOnlyScriptsAndTheGroupsThatHoldThem()
    {
        // Arrange
        string xml = Table(
            Entry(1, "Main", Script),
            Entry(2, "Group", null,
                Entry(3, "Inner", Script),
                Entry(4, "Health", "4 Bytes")),
            Entry(5, "Values", null,
                Entry(6, "Timer", "Float")));

        // Act
        CheatTable table = CheatTable.Parse(xml);

        // Assert
        Assert.Equal(
            [
                new TableScript(1, "Main", true, []),
                new TableScript(2, "Group", false, [new TableScript(3, "Inner", true, [])]),
            ],
            table.Scripts,
            TableScriptComparer.Instance);
    }

    [Fact]
    public void Parse_DescriptionWithQuotesAndRepeatedSpaces_CleansTheDescription()
    {
        // Arrange
        string xml = Table(Entry(1, "\"Core  / Geo   Stuff \"", Script));

        // Act
        CheatTable table = CheatTable.Parse(xml);

        // Assert
        Assert.Equal("Core / Geo Stuff", table.Scripts[0].Description);
    }

    [Fact]
    public void Parse_EntryWithoutNumericId_SkipsTheEntry()
    {
        // Arrange
        string xml = Table(
            "<CheatEntry><Description>\"No id\"</Description><VariableType>Auto Assembler Script</VariableType></CheatEntry>",
            Entry(7, "Kept", Script));

        // Act
        CheatTable table = CheatTable.Parse(xml);

        // Assert
        Assert.Equal([7], table.Scripts.Select(script => script.Id));
    }

    [Theory]
    [InlineData("<CheatTable><CheatEntries>")]
    [InlineData("<Other />")]
    public void Parse_DocumentThatIsNotACheatTable_ThrowsInvalidCheatTable(string xml)
    {
        // Act
        Exception? exception = Record.Exception(() => CheatTable.Parse(xml));

        // Assert
        Assert.IsType<InvalidCheatTableException>(exception);
    }

    [Theory]
    [InlineData(CoreDescription)]
    [InlineData("core scripts")]
    [InlineData("CORE")]
    public void IsRequired_TopLevelScriptNamedCore_IsRequired(string description)
    {
        // Arrange
        CheatTable table = CheatTable.Parse(Table(Entry(1, description, Script), Entry(2, "Extra", Script)));

        // Act
        bool required = table.IsRequired(1);

        // Assert
        Assert.True(required);
    }

    [Theory]
    [InlineData("Corner Fix")]
    [InlineData("Hardcore Mode")]
    [InlineData("Danny Stuff")]
    public void IsRequired_TopLevelScriptWithAnotherName_IsNotRequired(string description)
    {
        // Arrange
        CheatTable table = CheatTable.Parse(Table(Entry(1, description, Script)));

        // Act
        bool required = table.IsRequired(1);

        // Assert
        Assert.False(required);
    }

    [Fact]
    public void IsRequired_NestedScriptNamedCore_IsNotRequired()
    {
        // Arrange
        CheatTable table = CheatTable.Parse(Table(Entry(1, "Group", null, Entry(2, "Core", Script))));

        // Act
        bool required = table.IsRequired(2);

        // Assert
        Assert.False(required);
    }

    [Fact]
    public void IsRequired_CoreGroupThatCannotBeToggled_IsNotRequired()
    {
        // Arrange
        CheatTable table = CheatTable.Parse(Table(Entry(1, "Core", null, Entry(2, "Inner", Script))));

        // Act
        bool required = table.IsRequired(1);

        // Assert
        Assert.False(required);
    }

    [Fact]
    public void SelectionToEnable_NoSavedSelection_TurnsOnOnlyTheCore()
    {
        // Arrange
        CheatTable table = CheatTable.Parse(Table(Entry(2, "Extra", Script), Entry(1, CoreDescription, Script)));

        // Act
        IReadOnlyList<int> selection = table.SelectionToEnable(null);

        // Assert
        Assert.Equal([1], selection);
    }

    [Fact]
    public void SelectionToEnable_SavedSelectionWithoutTheCore_StillTurnsTheCoreOnFirst()
    {
        // Arrange
        CheatTable table = CheatTable.Parse(Table(Entry(1, CoreDescription, Script), Entry(2, "Extra", Script)));

        // Act
        IReadOnlyList<int> selection = table.SelectionToEnable([2]);

        // Assert
        Assert.Equal([1, 2], selection);
    }

    [Fact]
    public void SelectionToEnable_SavedSelectionWithTheCore_DoesNotRepeatIt()
    {
        // Arrange
        CheatTable table = CheatTable.Parse(Table(Entry(1, CoreDescription, Script), Entry(2, "Extra", Script)));

        // Act
        IReadOnlyList<int> selection = table.SelectionToEnable([2, 1]);

        // Assert
        Assert.Equal([1, 2], selection);
    }

    [Fact]
    public void SelectionToEnable_SavedIdsThatNoLongerExistOrCannotToggle_AreDropped()
    {
        // Arrange
        CheatTable table = CheatTable.Parse(Table(Entry(1, "Main", Script), Entry(2, "Group", null, Entry(3, "Inner", Script))));

        // Act
        IReadOnlyList<int> selection = table.SelectionToEnable([3, 99, 2]);

        // Assert
        Assert.Equal([3], selection);
    }

    [Fact]
    public void SelectionToEnable_TableWithoutCoreAndNoSavedSelection_TurnsNothingOn()
    {
        // Arrange
        CheatTable table = CheatTable.Parse(Table(Entry(1, "Main", Script)));

        // Act
        IReadOnlyList<int> selection = table.SelectionToEnable(null);

        // Assert
        Assert.Empty(selection);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(2, false)]
    [InlineData(4, false)]
    public void ContainsScript_Id_IsTrueOnlyForToggleableEntries(int id, bool expected)
    {
        // Arrange
        CheatTable table = CheatTable.Parse(Table(Entry(2, "Group", null, Entry(3, "Inner", Script))));

        // Act
        bool contains = table.ContainsScript(id);

        // Assert
        Assert.Equal(expected, contains);
    }

    private static string Table(params string[] entries) =>
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?><CheatTable CheatEngineTableVersion=\"46\"><CheatEntries>{string.Concat(entries)}</CheatEntries></CheatTable>";

    private static string Entry(int id, string description, string? variableType, params string[] children)
    {
        string type = variableType is null ? "<GroupHeader>1</GroupHeader>" : $"<VariableType>{variableType}</VariableType>";
        string nested = children.Length == 0 ? "" : $"<CheatEntries>{string.Concat(children)}</CheatEntries>";
        return $"<CheatEntry><ID>{id}</ID><Description>\"{description.Trim('"')}\"</Description>{type}{nested}</CheatEntry>";
    }

    private sealed class TableScriptComparer : IEqualityComparer<TableScript>
    {
        public static readonly TableScriptComparer Instance = new();

        public bool Equals(TableScript? left, TableScript? right) =>
            left is not null && right is not null
            && left.Id == right.Id
            && left.Description == right.Description
            && left.CanToggle == right.CanToggle
            && left.Children.SequenceEqual(right.Children, this);

        public int GetHashCode(TableScript script) => script.Id;
    }
}
