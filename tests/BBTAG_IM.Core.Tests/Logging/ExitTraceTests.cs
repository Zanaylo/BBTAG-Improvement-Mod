using BBTAG_IM.Core.Logging;

namespace BBTAG_IM.Core.Tests.Logging;

public class ExitTraceTests
{
    private const uint ModuleStart = 0x00400000;
    private const uint ModuleEnd = 0x02200000;

    [Fact]
    public void GameAddresses_StackWithGameAndOtherValues_KeepsOnlyTheGameOnesInOrder()
    {
        // Arrange
        uint[] stack = [0x00000001, 0x0077CC25, 0x7FFE0000, 0x0079DEE1, 0x02200000, 0x00400000];

        // Act
        IReadOnlyList<uint> addresses = ExitTrace.GameAddresses(stack, ModuleStart, ModuleEnd, 10);

        // Assert
        Assert.Equal([0x0077CC25u, 0x0079DEE1u, 0x00400000u], addresses);
    }

    [Fact]
    public void GameAddresses_MoreThanTheLimit_StopsAtTheLimit()
    {
        // Arrange
        uint[] stack = [0x00401000, 0x00402000, 0x00403000];

        // Act
        IReadOnlyList<uint> addresses = ExitTrace.GameAddresses(stack, ModuleStart, ModuleEnd, 2);

        // Assert
        Assert.Equal([0x00401000u, 0x00402000u], addresses);
    }

    [Fact]
    public void Describe_Addresses_ListsThemInHex()
    {
        // Arrange
        uint[] addresses = [0x0077CC25, 0x0079DEE1];

        // Act
        string text = ExitTrace.Describe(addresses);

        // Assert
        Assert.Equal("0x0077CC25 0x0079DEE1", text);
    }

    [Fact]
    public void Describe_NoAddresses_SaysNoneWereFound()
    {
        // Act
        string text = ExitTrace.Describe([]);

        // Assert
        Assert.Equal("none found", text);
    }
}
