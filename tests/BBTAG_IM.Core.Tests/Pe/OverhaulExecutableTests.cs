using BBTAG_IM.Core.Pe;

namespace BBTAG_IM.Core.Tests.Pe;

public class OverhaulExecutableTests
{
    [Fact]
    public void Is_SectionsWithExtend_ReturnsTrue()
    {
        // Arrange
        PeSection[] sections = [Section(".text"), Section(".extend")];

        // Act
        bool overhaul = OverhaulExecutable.Is(sections);

        // Assert
        Assert.True(overhaul);
    }

    [Fact]
    public void Is_StockSections_ReturnsFalse()
    {
        // Arrange
        PeSection[] sections = [Section(".text"), Section(".rdata"), Section(".data")];

        // Act
        bool overhaul = OverhaulExecutable.Is(sections);

        // Assert
        Assert.False(overhaul);
    }

    private static PeSection Section(string name) => new(name, 0x1000, 0x1000, 0x400, 0x1000, 0);
}
