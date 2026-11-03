using BBTAG_IM.Core.Selection;

namespace BBTAG_IM.Core.Tests.Selection;

public class ChoiceSearchTests
{
    private static readonly string[] Labels = ["NmlAtk5A", "NmlAtk2A", "CmnActStand", "408_dog"];

    [Fact]
    public void Matching_NothingTyped_ListsEveryChoice()
    {
        // Act
        IReadOnlyList<int> matching = ChoiceSearch.Matching(Labels, " ");

        // Assert
        Assert.Equal([0, 1, 2, 3], matching);
    }

    [Fact]
    public void Matching_PartOfTheLabel_ListsTheChoicesHoldingIt()
    {
        // Act
        IReadOnlyList<int> matching = ChoiceSearch.Matching(Labels, "atk");

        // Assert
        Assert.Equal([0, 1], matching);
    }

    [Fact]
    public void Matching_TextInAnotherCase_StillMatches()
    {
        // Act
        IReadOnlyList<int> matching = ChoiceSearch.Matching(Labels, "a");

        // Assert
        Assert.Equal([0, 1, 2], matching);
    }

    [Fact]
    public void Matching_LabelStartingWithTheText_ComesBeforeTheOthers()
    {
        // Act
        IReadOnlyList<int> matching = ChoiceSearch.Matching(Labels, "cmn");

        // Assert
        Assert.Equal([2], matching);
    }

    [Fact]
    public void Matching_TextStartingOneLabelAndInsideAnother_PutsTheStartFirst()
    {
        // Act
        IReadOnlyList<int> matching = ChoiceSearch.Matching(["NmlAtk5A", "AtkCharge"], "atk");

        // Assert
        Assert.Equal([1, 0], matching);
    }

    [Fact]
    public void Matching_NoLabelHoldsTheText_ListsNothing()
    {
        // Act
        IReadOnlyList<int> matching = ChoiceSearch.Matching(Labels, "zzz");

        // Assert
        Assert.Empty(matching);
    }
}
