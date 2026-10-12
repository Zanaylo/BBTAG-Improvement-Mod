using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Tests.Settings;

public class LauncherSettingsTests
{
    [Fact]
    public void SelectionFor_TableNameInAnotherCase_FindsTheSavedSelection()
    {
        // Arrange
        LauncherSettings settings = new() { ScriptSelections = { ["BBTAG.CT"] = [1, 2] } };

        // Act
        IReadOnlyList<int>? selection = settings.SelectionFor("bbtag.ct");

        // Assert
        Assert.Equal([1, 2], selection);
    }

    [Fact]
    public void SelectionFor_TableNeverSaved_ReturnsNull()
    {
        // Arrange
        LauncherSettings settings = new() { ScriptSelections = { ["BBTAG.CT"] = [1, 2] } };

        // Act
        IReadOnlyList<int>? selection = settings.SelectionFor("Other.CT");

        // Assert
        Assert.Null(selection);
    }

    [Fact]
    public void Remember_TableSavedInAnotherCase_ReplacesItsSelection()
    {
        // Arrange
        LauncherSettings settings = new() { ScriptSelections = { ["bbtag.ct"] = [1] } };

        // Act
        settings.Remember("BBTAG.CT", [5, 6]);

        // Assert
        Assert.Equal([5, 6], Assert.Single(settings.ScriptSelections).Value);
    }

    [Fact]
    public void IsModEnabled_ModNeverTurnedOff_ReturnsTrue()
    {
        // Arrange
        LauncherSettings settings = new();

        // Act
        bool isEnabled = settings.IsModEnabled("BBTAG-OverHaul");

        // Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void IsModEnabled_ModTurnedOffInAnotherCase_ReturnsFalse()
    {
        // Arrange
        LauncherSettings settings = new();
        settings.EnableMod("bbtag-overhaul", false);

        // Act
        bool isEnabled = settings.IsModEnabled("BBTAG-OverHaul");

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void EnableMod_ModTurnedOffBefore_TurnsItBackOn()
    {
        // Arrange
        LauncherSettings settings = new();
        settings.EnableMod("BBTAG-OverHaul", false);

        // Act
        settings.EnableMod("BBTAG-OverHaul", true);

        // Assert
        Assert.Empty(settings.DisabledMods);
    }

    [Fact]
    public void EnabledOf_OneModTurnedOff_KeepsTheOthersInTheirOrder()
    {
        // Arrange
        LauncherSettings settings = new();
        settings.EnableMod("B", false);
        ModFolder[] mods = [Mod("A"), Mod("B"), Mod("C")];

        // Act
        IReadOnlyList<ModFolder> enabled = settings.EnabledOf(mods);

        // Assert
        Assert.Equal(["A", "C"], enabled.Select(mod => mod.Name));
    }

    private static ModFolder Mod(string name) => new(name, $@"D:\BBTAG\mods\{name}", [], null, null, []);
}
