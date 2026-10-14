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
}
