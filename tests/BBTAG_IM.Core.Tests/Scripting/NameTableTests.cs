using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Tests.Scripting;

public class NameTableTests
{
    [Fact]
    public void NameOf_UniqueName_ReturnsTheName()
    {
        // Arrange
        NameTable table = new([new(1, "LAST")]);

        // Act
        string? name = table.NameOf(1);

        // Assert
        Assert.Equal("LAST", name);
    }

    [Fact]
    public void NameOf_NameSharedByTwoValues_ReturnsNull()
    {
        // Arrange
        NameTable table = new([new(182, "INPUT_632146"), new(249, "INPUT_632146")]);

        // Act
        string? name = table.NameOf(182);

        // Assert
        Assert.Null(name);
    }

    [Fact]
    public void ValueOf_DifferentCase_ReturnsTheValue()
    {
        // Arrange
        NameTable table = new([new(10, "AIR_VERTICAL")]);

        // Act
        int? value = table.ValueOf("air_vertical");

        // Assert
        Assert.Equal(10, value);
    }

    [Fact]
    public void With_OverrideForTheSameValue_UsesTheOverride()
    {
        // Arrange
        NameTable global = new([new(51, "Slot51")]);
        NameTable character = new([new(51, "IsInOverdrive")]);

        // Act
        NameTable merged = global.With(character);

        // Assert
        Assert.Equal("IsInOverdrive", merged.NameOf(51));
    }
    [Fact]
    public void DescriptionOf_ValueNamedWithADescription_ReturnsTheDescription()
    {
        // Arrange
        NameTable table = NameTable.Of([new NamedValue(0x2000, "MOVE_GROUND", "Only on the ground.")]);

        // Act
        string? description = table.DescriptionOf(0x2000);

        // Assert
        Assert.Equal("Only on the ground.", description);
    }

    [Fact]
    public void With_OverrideWithADescription_KeepsTheDescription()
    {
        // Arrange
        NameTable global = new([new(1, "LAST")]);
        NameTable overrides = NameTable.Of([new NamedValue(2, "PARENT", "The object that made this one.")]);

        // Act
        NameTable merged = global.With(overrides);

        // Assert
        Assert.Equal("The object that made this one.", merged.DescriptionOf(2));
    }

    [Fact]
    public void Named_NameSharedByTwoValues_IsLeftOut()
    {
        // Arrange
        NameTable table = new([new(182, "INPUT_632146"), new(249, "INPUT_632146"), new(172, "INPUT_236")]);

        // Act
        IEnumerable<NamedValue> named = table.Named;

        // Assert
        Assert.Equal(["INPUT_236"], named.Select(value => value.Name));
    }
}
