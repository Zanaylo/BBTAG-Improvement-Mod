using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Tests.Scripting;

public class StockCommandSizesTests
{
    [Theory]
    [InlineData(0, 36)]
    [InlineData(2, 40)]
    [InlineData(2074, 20)]
    [InlineData(23155, 44)]
    [InlineData(30093, 24)]
    public void Load_KnownCommand_HasTheGameSize(int id, int expected)
    {
        // Act
        IReadOnlyDictionary<int, int> sizes = StockCommandSizes.Load();

        // Assert
        Assert.Equal(expected, sizes[id]);
    }

    [Fact]
    public void Load_EveryEntryOfTheGameTable_IsRead()
    {
        // Act
        IReadOnlyDictionary<int, int> sizes = StockCommandSizes.Load();

        // Assert
        Assert.Equal(1166, sizes.Count);
    }
}
