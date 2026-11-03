using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Tests.Palettes;

public class EntrySelectionTests
{
    private const int FirstEntry = 1;

    public EntrySelection CreateEntrySelection() => new(FirstEntry);

    [Fact]
    public void New_Entry_SelectsOnlyThatEntry()
    {
        // Act
        EntrySelection selection = CreateEntrySelection();

        // Assert
        Assert.Equal([FirstEntry], selection.Entries);
    }

    [Fact]
    public void Pick_Entry_SelectsOnlyThatEntry()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();
        selection.Toggle(5);

        // Act
        selection.Pick(9);

        // Assert
        Assert.Equal([9], selection.Entries);
    }

    [Fact]
    public void Pick_Entry_FocusesIt()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();

        // Act
        selection.Pick(9);

        // Assert
        Assert.Equal(9, selection.Focus);
    }

    [Fact]
    public void Toggle_UnselectedEntry_AddsItInEntryOrder()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();
        selection.Pick(9);

        // Act
        selection.Toggle(4);

        // Assert
        Assert.Equal([4, 9], selection.Entries);
    }

    [Fact]
    public void Toggle_UnselectedEntry_FocusesIt()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();

        // Act
        selection.Toggle(4);

        // Assert
        Assert.Equal(4, selection.Focus);
    }

    [Fact]
    public void Toggle_SelectedEntry_RemovesIt()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();
        selection.Toggle(4);

        // Act
        selection.Toggle(FirstEntry);

        // Assert
        Assert.Equal([4], selection.Entries);
    }

    [Fact]
    public void Toggle_FocusedEntry_FocusesTheFirstEntryLeft()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();
        selection.Toggle(4);
        selection.Toggle(7);

        // Act
        selection.Toggle(7);

        // Assert
        Assert.Equal(FirstEntry, selection.Focus);
    }

    [Fact]
    public void Toggle_OnlySelectedEntry_KeepsIt()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();

        // Act
        selection.Toggle(FirstEntry);

        // Assert
        Assert.Equal([FirstEntry], selection.Entries);
    }

    [Fact]
    public void ExtendTo_LaterEntry_SelectsEverythingFromTheLastClick()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();
        selection.Pick(3);

        // Act
        selection.ExtendTo(6);

        // Assert
        Assert.Equal([3, 4, 5, 6], selection.Entries);
    }

    [Fact]
    public void ExtendTo_EarlierEntry_SelectsBackwards()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();
        selection.Pick(6);

        // Act
        selection.ExtendTo(4);

        // Assert
        Assert.Equal([4, 5, 6], selection.Entries);
    }

    [Fact]
    public void ExtendTo_Twice_StartsBothTimesFromTheLastClick()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();
        selection.Pick(6);
        selection.ExtendTo(9);

        // Act
        selection.ExtendTo(7);

        // Assert
        Assert.Equal([6, 7], selection.Entries);
    }

    [Fact]
    public void ExtendTo_AfterAToggle_StartsFromTheToggledEntry()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();
        selection.Toggle(10);

        // Act
        selection.ExtendTo(12);

        // Assert
        Assert.Equal([10, 11, 12], selection.Entries);
    }

    [Fact]
    public void ExtendTo_Entry_FocusesIt()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();

        // Act
        selection.ExtendTo(4);

        // Assert
        Assert.Equal(4, selection.Focus);
    }

    [Fact]
    public void Contains_UnselectedEntry_ReturnsFalse()
    {
        // Arrange
        EntrySelection selection = CreateEntrySelection();

        // Act
        bool contains = selection.Contains(4);

        // Assert
        Assert.False(contains);
    }
}
