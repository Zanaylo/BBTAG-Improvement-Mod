using System.Globalization;
using System.Text.Json.Nodes;
using BBTAG_IM.Core.Scripting.Importing;

namespace BBTAG_IM.Core.Tests.Scripting.Importing;

public class CommandNameImportTests
{
    private const int ModNamedCommand = 1047;

    [Fact]
    public void Merge_CommandWithoutName_TakesTheBbcfName()
    {
        // Arrange
        string commands = """{ "1013": { "format": "i" } }""";
        BbcfTable table = Table((1013, "SetSpeedX", ["S32"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((1013, 8)));

        // Assert
        Assert.Equal("SetSpeedX", Entry(result, 1013)["name"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_UnknownName_IsReplaced()
    {
        // Arrange
        string commands = """{ "1013": { "name": "Unknown1013", "format": "i" } }""";
        BbcfTable table = Table((1013, "SetSpeedX", ["S32"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((1013, 8)));

        // Assert
        Assert.Equal("SetSpeedX", Entry(result, 1013)["name"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_NamedCommand_KeepsItsName()
    {
        // Arrange
        string commands = """{ "1013": { "name": "physicsXImpulse", "format": "i" } }""";
        BbcfTable table = Table((1013, "SetSpeedX", ["S32"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((1013, 8)));

        // Assert
        Assert.Equal("physicsXImpulse", Entry(result, 1013)["name"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_CommandWithoutName_KeepsItsOtherFields()
    {
        // Arrange
        string commands = """{ "18": { "type_check": [1], "format": "3i" } }""";
        BbcfTable table = Table((18, "GotoIf", ["S32", "S32", "S32"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((18, 16)));

        // Assert
        Assert.Equal("3i", Entry(result, 18)["format"]!.GetValue<string>());
        Assert.Equal(1, Entry(result, 18)["type_check"]![0]!.GetValue<int>());
    }

    [Fact]
    public void Merge_CommandMissing_IsAddedWithTheBbcfFormat()
    {
        // Arrange
        string commands = "{}";
        BbcfTable table = Table((22, "ActionRequestNotSame", ["C32BYTE"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((22, 36)));

        // Assert
        Assert.Equal("ActionRequestNotSame", Entry(result, 22)["name"]!.GetValue<string>());
        Assert.Equal("32s", Entry(result, 22)["format"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_CommandMissingWithAnotherLayout_IsAddedWithTheGameSize()
    {
        // Arrange
        string commands = "{}";
        BbcfTable table = Table((9007, "SubVoice", ["C16BYTE", "U8"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((9007, 24)));

        // Assert
        Assert.Equal(24, Entry(result, 9007)["size"]!.GetValue<int>());
        Assert.Null(Entry(result, 9007)["format"]);
    }

    [Fact]
    public void Merge_EntryWithoutLayout_GetsTheGameSize()
    {
        // Arrange
        string commands = """{ "30004": { "name": "" } }""";
        BbcfTable table = Table((30004, "LinkCharaModelAngle", ["S32"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((30004, 16)));

        // Assert
        Assert.Equal(16, Entry(result, 30004)["size"]!.GetValue<int>());
    }

    [Fact]
    public void Merge_CommandNotInTheGame_IsLeftOut()
    {
        // Arrange
        string commands = "{}";
        BbcfTable table = Table((2622, "BbcfOnly", []));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((0, 36)));

        // Assert
        Assert.Null(JsonNode.Parse(result.Json)!["2622"]);
    }

    [Fact]
    public void Merge_NameUsedByAnotherCommand_IsSkipped()
    {
        // Arrange
        string commands = """{ "47": { "name": "PrivateFunction", "format": "" }, "23030": { "format": "i" } }""";
        BbcfTable table = Table((23030, "privatefunction", ["S32"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((47, 4), (23030, 8)));

        // Assert
        Assert.Null(Entry(result, 23030)["name"]);
        Assert.Single(result.Skipped);
    }

    [Fact]
    public void Merge_CommandTheModNames_KeepsTheModName()
    {
        // Arrange
        string commands = """{ "1047": { "format": "i" } }""";
        BbcfTable table = Table((ModNamedCommand, "AddInertia", ["S32"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((ModNamedCommand, 8)));

        // Assert
        Assert.Null(Entry(result, ModNamedCommand)["name"]);
    }

    [Fact]
    public void Merge_NameThatIsNotAnIdentifier_IsSkipped()
    {
        // Arrange
        string commands = """{ "1013": { "format": "i" } }""";
        BbcfTable table = Table((1013, "Set Speed", ["S32"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((1013, 8)));

        // Assert
        Assert.Null(Entry(result, 1013)["name"]);
        Assert.Single(result.Skipped);
    }

    [Fact]
    public void Merge_NamedAndAddedCommands_AreCounted()
    {
        // Arrange
        string commands = """{ "1013": { "format": "i" } }""";
        BbcfTable table = Table((1013, "SetSpeedX", ["S32"]), (22, "ActionRequestNotSame", ["C32BYTE"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((22, 36), (1013, 8)));

        // Assert
        Assert.Equal(2, result.Named);
        Assert.Equal(1, result.Added);
    }

    [Fact]
    public void Merge_Commands_AreWrittenInNumberOrder()
    {
        // Arrange
        string commands = """{ "1013": { "name": "physicsXImpulse", "format": "i" } }""";
        BbcfTable table = Table((22, "ActionRequestNotSame", ["C32BYTE"]));

        // Act
        NameImportResult result = CommandNameImport.Merge(commands, table, Sizes((22, 36), (1013, 8)));

        // Assert
        Assert.Equal(["22", "1013"], JsonNode.Parse(result.Json)!.AsObject().Select(pair => pair.Key));
    }

    private static JsonNode Entry(NameImportResult result, int id) =>
        JsonNode.Parse(result.Json)![id.ToString(CultureInfo.InvariantCulture)]!;

    private static BbcfTable Table(params (int Id, string Name, string[] Types)[] commands) =>
        new(commands.ToDictionary(command => command.Id, command => new BbcfCommand(command.Name, command.Types)), new Dictionary<int, string>());

    private static Dictionary<int, int> Sizes(params (int Id, int Size)[] sizes) =>
        sizes.ToDictionary(size => size.Id, size => size.Size);
}
