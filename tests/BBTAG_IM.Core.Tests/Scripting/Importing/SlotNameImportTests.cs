using System.Globalization;
using System.Text.Json.Nodes;
using BBTAG_IM.Core.Scripting.Importing;

namespace BBTAG_IM.Core.Tests.Scripting.Importing;

public class SlotNameImportTests
{
    [Fact]
    public void Merge_SlotWithoutName_TakesTheVariableNameWithoutItsPrefix()
    {
        // Arrange
        string slots = "{}";
        BbcfTable table = Table((4, "VAR_PlayerVal0"));

        // Act
        NameImportResult result = SlotNameImport.Merge(slots, table);

        // Assert
        Assert.Equal("PlayerVal0", Slot(result, 4));
    }

    [Fact]
    public void Merge_NamedSlot_KeepsItsName()
    {
        // Arrange
        string slots = """{ "12": "XVelocity" }""";
        BbcfTable table = Table((12, "VAR_SpeedX"));

        // Act
        NameImportResult result = SlotNameImport.Merge(slots, table);

        // Assert
        Assert.Equal("XVelocity", Slot(result, 12));
    }

    [Fact]
    public void Merge_SlotPastTheOnesBothGamesShare_IsLeftOut()
    {
        // Arrange
        string slots = "{}";
        BbcfTable table = Table((107, "VAR_IsUkemiEnable"));

        // Act
        NameImportResult result = SlotNameImport.Merge(slots, table);

        // Assert
        Assert.Null(Slot(result, 107));
    }

    [Fact]
    public void Merge_NameUsedByAnotherSlot_IsSkipped()
    {
        // Arrange
        string slots = """{ "37": "IsLand" }""";
        BbcfTable table = Table((36, "VAR_IsLand"));

        // Act
        NameImportResult result = SlotNameImport.Merge(slots, table);

        // Assert
        Assert.Null(Slot(result, 36));
        Assert.Single(result.Skipped);
    }

    [Fact]
    public void Merge_NamedSlots_AreCounted()
    {
        // Arrange
        string slots = """{ "2": "ActionMark" }""";
        BbcfTable table = Table((2, "VAR_ActionMark"), (4, "VAR_PlayerVal0"), (5, "VAR_PlayerVal1"));

        // Act
        NameImportResult result = SlotNameImport.Merge(slots, table);

        // Assert
        Assert.Equal(2, result.Named);
        Assert.Equal(2, result.Added);
    }

    [Fact]
    public void Merge_Slots_AreWrittenInNumberOrder()
    {
        // Arrange
        string slots = """{ "12": "XVelocity" }""";
        BbcfTable table = Table((4, "VAR_PlayerVal0"));

        // Act
        NameImportResult result = SlotNameImport.Merge(slots, table);

        // Assert
        Assert.Equal(["4", "12"], JsonNode.Parse(result.Json)!.AsObject().Select(pair => pair.Key));
    }

    private static string? Slot(NameImportResult result, int slot) =>
        JsonNode.Parse(result.Json)![slot.ToString(CultureInfo.InvariantCulture)]?.GetValue<string>();

    private static BbcfTable Table(params (int Slot, string Name)[] variables) =>
        new(new Dictionary<int, BbcfCommand>(), variables.ToDictionary(variable => variable.Slot, variable => variable.Name));
}
