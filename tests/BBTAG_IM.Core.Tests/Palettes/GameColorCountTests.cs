using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Palettes;

public class GameColorCountTests
{
    private const uint CodeAddress = 0x401000;
    private const int CodeSize = 0x100;
    private const uint SiteOffset = 0x40;
    private const uint Limit = CodeAddress + SiteOffset + 8;
    private const byte StockLimit = 20;
    private const byte OverhaulLimit = 99;

    private readonly FakeGameMemory _memoryMock = new();

    public GameColorCount CreateGameColorCount(uint? limit = Limit) => new(_memoryMock, limit);

    [Fact]
    public void LocateLimit_CodeWithTheColorLimitCheck_ReturnsTheAddressOfTheLimit()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeAddress, CreateLimitCode().ToArray())]);

        // Act
        uint limit = GameColorCount.LocateLimit(image);

        // Assert
        Assert.Equal(Limit, limit);
    }

    [Fact]
    public void LocateLimit_CodeWithoutTheCheck_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeAddress, new GameCode(CodeSize).ToArray())]);

        // Act
        Exception? exception = Record.Exception(() => GameColorCount.LocateLimit(image));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Color limit was not found in the game code. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void Current_StockGame_ReturnsTwentyColors()
    {
        // Arrange
        LimitIs(StockLimit);
        GameColorCount colors = CreateGameColorCount();

        // Act
        int count = colors.Current;

        // Assert
        Assert.Equal(20, count);
    }

    [Fact]
    public void Current_LimitRaisedByAPatch_ReturnsTheRaisedCount()
    {
        // Arrange
        LimitIs(OverhaulLimit);
        GameColorCount colors = CreateGameColorCount();

        // Act
        int count = colors.Current;

        // Assert
        Assert.Equal(99, count);
    }

    [Fact]
    public void Current_LimitBelowTheStockCount_ReturnsTheStockCount()
    {
        // Arrange
        LimitIs(3);
        GameColorCount colors = CreateGameColorCount();

        // Act
        int count = colors.Current;

        // Assert
        Assert.Equal(20, count);
    }

    [Fact]
    public void Current_LimitNotFound_ReturnsTheStockCount()
    {
        // Arrange
        GameColorCount colors = CreateGameColorCount(limit: null);

        // Act
        int count = colors.Current;

        // Assert
        Assert.Equal(20, count);
    }

    [Fact]
    public void Current_LimitUnreadable_ReturnsTheStockCount()
    {
        // Arrange
        GameColorCount colors = CreateGameColorCount();

        // Act
        int count = colors.Current;

        // Assert
        Assert.Equal(20, count);
    }

    private void LimitIs(byte value) => _memoryMock.Map(Limit, 1).BytesAt(Limit, [value]);

    private static GameCode CreateLimitCode() =>
        new GameCode(CodeSize).Bytes(SiteOffset, "E8 0A 8C DB FF 83 78 08 14 72 55 57 8B CB E8 FC 8B DB FF");
}
