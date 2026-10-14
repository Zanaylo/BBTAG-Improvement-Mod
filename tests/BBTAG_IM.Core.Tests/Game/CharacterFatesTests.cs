using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Game;

public class CharacterFatesTests
{
    private const uint RosterGetter = 0x00401100;
    private const uint FateGetter = 0x00401200;
    private const uint Roster = 0x00C37768;
    private const uint FateTable = 0x00AC1730;
    private const uint LastIndexOffset = 0x2E00;
    private const int LastIndex = 140;
    private const int Hyde = 26;
    private const int Kohaku = 120;

    private readonly FakeGameMemory _memoryMock = new();

    public CharacterFatesTests()
    {
        _memoryMock.Map(RosterGetter, 8).BytesAt(RosterGetter, [0xB8, 0, 0, 0, 0, GameCode.Return]).UInt32At(RosterGetter + 1, Roster);
        _memoryMock.Map(Roster + LastIndexOffset, sizeof(int)).Int32At(Roster + LastIndexOffset, LastIndex);
        _memoryMock.Map(FateTable, LastIndex + 1);
    }

    public CharacterFates CreateCharacterFates(uint? fateGetter = FateGetter) =>
        new(_memoryMock, new CharacterRoster(_memoryMock, RosterGetter), fateGetter);

    [Fact]
    public void Of_FunctionReadingAFateTable_ReturnsTheFateOfTheTable()
    {
        // Arrange
        FunctionReadsTable();
        _memoryMock.BytesAt(FateTable + Kohaku, [(byte)Fate.MeltyBlood]);
        CharacterFates fates = CreateCharacterFates();

        // Act
        Fate? fate = fates.Of(Kohaku);

        // Assert
        Assert.Equal(Fate.MeltyBlood, fate);
    }

    [Fact]
    public void Of_FunctionReadingAFateTableAndIndexPastTheRoster_ReturnsNull()
    {
        // Arrange
        FunctionReadsTable();
        CharacterFates fates = CreateCharacterFates();

        // Act
        Fate? fate = fates.Of(LastIndex + 1);

        // Assert
        Assert.Null(fate);
    }

    [Fact]
    public void Of_TableValueThatIsNoFate_ReturnsNull()
    {
        // Arrange
        FunctionReadsTable();
        _memoryMock.BytesAt(FateTable + Kohaku, [42]);
        CharacterFates fates = CreateCharacterFates();

        // Act
        Fate? fate = fates.Of(Kohaku);

        // Assert
        Assert.Null(fate);
    }

    [Fact]
    public void Of_StockFunction_ReturnsTheFateOfTheStockCharacter()
    {
        // Arrange
        _memoryMock.Map(FateGetter, 0x20).BytesAt(FateGetter, [0x55, 0x8B, 0xEC, 0x8B, 0x45, 0x08, 0x33, 0xC9, 0x83, 0xF8, 0x2D, 0x77, 0x13, 0x0F, 0xB6, 0x80, 0x50, 0x45, 0x67, 0x00, 0xFF, 0x24, 0x85]);
        CharacterFates fates = CreateCharacterFates();

        // Act
        Fate? fate = fates.Of(Hyde);

        // Assert
        Assert.Equal(Fate.UnderNight, fate);
    }

    [Fact]
    public void Of_FunctionNotFound_ReturnsTheFateOfTheStockCharacter()
    {
        // Arrange
        CharacterFates fates = CreateCharacterFates(fateGetter: null);

        // Act
        Fate? fate = fates.Of(Hyde);

        // Assert
        Assert.Equal(Fate.UnderNight, fate);
    }

    [Theory]
    [InlineData(0, Fate.BlazBlue)]
    [InlineData(16, Fate.Persona)]
    [InlineData(36, Fate.RWBY)]
    [InlineData(40, Fate.BlazBlue)]
    [InlineData(43, Fate.ArcanaHeart)]
    [InlineData(48, Fate.AkatsukiBlitzkampf)]
    [InlineData(51, Fate.SenranKagura)]
    [InlineData(52, Fate.AkatsukiBlitzkampf)]
    public void Of_FunctionNotFoundAndStockCharacter_ReturnsItsSeries(int index, Fate expected)
    {
        // Arrange
        CharacterFates fates = CreateCharacterFates(fateGetter: null);

        // Act
        Fate? fate = fates.Of(index);

        // Assert
        Assert.Equal(expected, fate);
    }

    [Fact]
    public void Of_FunctionNotFoundAndCharacterTheGameDoesNotShip_ReturnsNull()
    {
        // Arrange
        CharacterFates fates = CreateCharacterFates(fateGetter: null);

        // Act
        Fate? fate = fates.Of(Kohaku);

        // Assert
        Assert.Null(fate);
    }

    private void FunctionReadsTable()
    {
        _memoryMock.Map(FateGetter, 0x20)
            .BytesAt(FateGetter, [0x55, 0x8B, 0xEC, 0x8B, 0x45, 0x08, 0x33, 0xC9, 0x90, 0x90, 0x90, 0x90, 0x90, 0x0F, 0xB6, 0x80, 0, 0, 0, 0, 0x5D, 0xC2, 0x04, 0x00])
            .UInt32At(FateGetter + 16, FateTable);
    }
}
