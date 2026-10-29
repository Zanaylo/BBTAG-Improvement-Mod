using BBTAG_IM.Core.Roster;

namespace BBTAG_IM.Core.Tests.Roster;

public class RosterPlanTests
{
    private static readonly IReadOnlyList<string> StockCodes = Enumerable.Range(0, 53).Select(index => $"c{index:D2}").Append("bta").Append("bcr").ToArray();

    [Fact]
    public void Create_NewCharacter_TakesTheFirstFreeIndexAndItsTemplateIndex()
    {
        // Arrange
        CharacterManifest manifest = Manifest("rar", "c05");

        // Act
        RosterPlan plan = RosterPlan.Create([manifest], StockCodes);

        // Assert
        RosterAddition addition = Assert.Single(plan.Additions);
        Assert.Equal((55, 5, manifest), (addition.Index, addition.TemplateIndex, addition.Manifest));
    }

    [Fact]
    public void Create_TwoCharacters_TakeConsecutiveIndexes()
    {
        // Act
        RosterPlan plan = RosterPlan.Create([Manifest("rar", "c00"), Manifest("rgx", "c00")], StockCodes);

        // Assert
        Assert.Equal([55, 56], plan.Additions.Select(addition => addition.Index));
    }

    [Fact]
    public void Create_UnknownTemplate_LeavesTheCharacterOutWithAProblem()
    {
        // Act
        RosterPlan plan = RosterPlan.Create([Manifest("rar", "zzz")], StockCodes);

        // Assert
        Assert.Empty(plan.Additions);
        Assert.Equal(["rar was left out: its template zzz is not in the roster."], plan.Problems);
    }

    [Fact]
    public void Create_CodeAlreadyInTheRoster_LeavesTheCharacterOutWithAProblem()
    {
        // Act
        RosterPlan plan = RosterPlan.Create([Manifest("bta", "c00")], StockCodes);

        // Assert
        Assert.Empty(plan.Additions);
        Assert.Equal(["bta was left out: the roster already has that code."], plan.Problems);
    }

    [Fact]
    public void Create_SameCodeTwice_KeepsTheFirstOne()
    {
        // Arrange
        CharacterManifest first = Manifest("rar", "c00");

        // Act
        RosterPlan plan = RosterPlan.Create([first, Manifest("rar", "c01")], StockCodes);

        // Assert
        Assert.Equal(first, Assert.Single(plan.Additions).Manifest);
        Assert.Equal(["rar was left out: a mod higher in the list already adds that code."], plan.Problems);
    }

    [Fact]
    public void Create_MoreCharactersThanFreeIndexes_LeavesTheRestOut()
    {
        // Arrange
        CharacterManifest[] manifests = Enumerable.Range(0, 9).Select(number => Manifest($"n{number:D2}", "c00")).ToArray();

        // Act
        RosterPlan plan = RosterPlan.Create(manifests, StockCodes);

        // Assert
        Assert.Equal(62, plan.Additions[^1].Index);
        Assert.Equal(["n08 was left out: the roster has room for 8 new characters."], plan.Problems);
    }

    private static CharacterManifest Manifest(string code, string template) =>
        new(code, template, code.ToUpperInvariant(), null, Series.BlazBlue, new CharacterNames(code, code, code));
}
