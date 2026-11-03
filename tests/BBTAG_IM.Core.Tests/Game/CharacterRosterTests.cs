using System.Text;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Game;

public class CharacterRosterTests
{
    private const uint CodeAddress = 0x401000;
    private const int CodeSize = 0x200;
    private const uint GetterOffset = 0x100;
    private const uint Getter = CodeAddress + GetterOffset;
    private const uint Table = 0x00C37768;
    private const uint PatchedTable = 0x30000000;
    private const uint RecordLength = 0xB8;
    private const uint ShortNameOffset = 0x40;
    private const uint LastIndexOffset = 0x2E00;
    private const uint SpriteFolderOffset = 0xB0;
    private const int Ragna = 0;
    private const int Take = 53;
    private const int Kohaku = 120;

    private readonly FakeGameMemory _memoryMock = new();

    public CharacterRoster CreateCharacterRoster(uint? getter = Getter) => new(_memoryMock, getter);

    [Fact]
    public void LocateGetter_CodeWithTheRosterCall_ReturnsTheGetter()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeAddress, CreateRosterCode().ToArray())]);

        // Act
        uint getter = CharacterRoster.LocateGetter(image);

        // Assert
        Assert.Equal(Getter, getter);
    }

    [Fact]
    public void LocateGetter_GetterThatLoadsNoAddress_ThrowsGameLayoutException()
    {
        // Arrange
        byte[] code = CreateRosterCode().ToArray();
        code[GetterOffset] = GameCode.Return;
        CodeImage image = new([new CodeBlock(CodeAddress, code)]);

        // Act
        Exception? exception = Record.Exception(() => CharacterRoster.LocateGetter(image));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("The Character roster getter holds no object address. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void Identify_CharacterOfTheGame_ReturnsItsPaletteFolderName()
    {
        // Arrange
        CharacterRoster roster = CreateCharacterRoster(getter: null);

        // Act
        CharacterIdentity identity = roster.Identify(Ragna);

        // Assert
        Assert.Equal(new CharacterIdentity(Ragna, "Ragna", "Ragna"), identity);
    }

    [Fact]
    public void CodeOf_CharacterOfTheGame_ReturnsItsFileCode()
    {
        // Arrange
        RosterAt(Table);
        RecordAt(Table, Ragna, "brg", "B_RAGNA");
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        string? code = roster.CodeOf(Ragna);

        // Assert
        Assert.Equal("brg", code);
    }

    [Fact]
    public void CodeOf_RosterNotFound_ReturnsNull()
    {
        // Arrange
        CharacterRoster roster = CreateCharacterRoster(getter: null);

        // Act
        string? code = roster.CodeOf(Ragna);

        // Assert
        Assert.Null(code);
    }

    [Fact]
    public void Identify_ModdedCharacter_UsesItsCodeAsTheFolderAndItsShortNameAsTheName()
    {
        // Arrange
        RosterAt(Table);
        RecordAt(Table, Kohaku, "mko", "M_KOHAKU");
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        CharacterIdentity identity = roster.Identify(Kohaku);

        // Assert
        Assert.Equal(new CharacterIdentity(Kohaku, "mko", "Kohaku (mko)"), identity);
    }

    [Fact]
    public void Identify_ShortNameOfSeveralWords_ReturnsEachWordCapitalized()
    {
        // Arrange
        RosterAt(Table);
        RecordAt(Table, Kohaku, "mko", "M_NECO_ARC");
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        CharacterIdentity identity = roster.Identify(Kohaku);

        // Assert
        Assert.Equal("Neco Arc (mko)", identity.Name);
    }

    [Fact]
    public void Identify_RecordWithoutShortName_UsesTheCodeAsTheName()
    {
        // Arrange
        RosterAt(Table);
        RecordAt(Table, Kohaku, "mko", "");
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        CharacterIdentity identity = roster.Identify(Kohaku);

        // Assert
        Assert.Equal(new CharacterIdentity(Kohaku, "mko", "mko"), identity);
    }

    [Fact]
    public void Identify_RosterMovedByAPatch_ReadsTheTableTheGetterNowReturns()
    {
        // Arrange
        RosterAt(PatchedTable);
        RecordAt(Table, Kohaku, "old", "M_OLD");
        RecordAt(PatchedTable, Kohaku, "mko", "M_KOHAKU");
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        CharacterIdentity identity = roster.Identify(Kohaku);

        // Assert
        Assert.Equal("mko", identity.Folder);
    }

    [Fact]
    public void Identify_RecordHoldsNoCode_ReturnsANumberedCharacter()
    {
        // Arrange
        RosterAt(Table);
        RecordAt(Table, Kohaku, "\u0096ko", "M_KOHAKU");
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        CharacterIdentity identity = roster.Identify(Kohaku);

        // Assert
        Assert.Equal(new CharacterIdentity(Kohaku, "Character 120", "Character 120"), identity);
    }

    [Fact]
    public void Identify_RosterNotFound_ReturnsANumberedCharacter()
    {
        // Arrange
        CharacterRoster roster = CreateCharacterRoster(getter: null);

        // Act
        CharacterIdentity identity = roster.Identify(Kohaku);

        // Assert
        Assert.Equal("Character 120", identity.Folder);
    }

    [Fact]
    public void Identify_GetterOverwrittenByAJump_ReturnsANumberedCharacter()
    {
        // Arrange
        _memoryMock.Map(Getter, 8).BytesAt(Getter, [0xE9, 0x00, 0x00, 0x00, 0x30, 0xC3]);
        RecordAt(PatchedTable, Kohaku, "mko", "M_KOHAKU");
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        CharacterIdentity identity = roster.Identify(Kohaku);

        // Assert
        Assert.Equal("Character 120", identity.Folder);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(120, true)]
    [InlineData(121, false)]
    [InlineData(-1, false)]
    public void Contains_RosterEndingAtAnIndex_AcceptsOnlyIndexesUpToIt(int index, bool expected)
    {
        // Arrange
        RosterAt(Table);
        LastIndexAt(Table, Kohaku);
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        bool contains = roster.Contains(index);

        // Assert
        Assert.Equal(expected, contains);
    }

    [Fact]
    public void LastIndex_RosterEndingAtAnIndex_ReturnsIt()
    {
        // Arrange
        RosterAt(Table);
        LastIndexAt(Table, Kohaku);
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        int? lastIndex = roster.LastIndex;

        // Assert
        Assert.Equal(Kohaku, lastIndex);
    }

    [Fact]
    public void LastIndex_RosterNotFound_ReturnsNull()
    {
        // Arrange
        CharacterRoster roster = CreateCharacterRoster(getter: null);

        // Act
        int? lastIndex = roster.LastIndex;

        // Assert
        Assert.Null(lastIndex);
    }

    [Fact]
    public void Contains_RosterNotFound_AcceptsNoIndex()
    {
        // Arrange
        CharacterRoster roster = CreateCharacterRoster(getter: null);

        // Act
        bool contains = roster.Contains(Ragna);

        // Assert
        Assert.False(contains);
    }

    [Fact]
    public void Contains_RosterCountUnreadable_AcceptsNoIndex()
    {
        // Arrange
        RosterAt(Table);
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        bool contains = roster.Contains(Ragna);

        // Assert
        Assert.False(contains);
    }

    private void LastIndexAt(uint table, int lastIndex) => _memoryMock.Map(table + LastIndexOffset, sizeof(int)).Int32At(table + LastIndexOffset, lastIndex);

    [Fact]
    public void IndexOf_CodeInTheRoster_ReturnsItsIndex()
    {
        // Arrange
        RosterAt(Table, lastIndex: Kohaku);
        RecordAt(Table, Take, "bta", "TAKEMIKAZUCHI");
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        int? index = roster.IndexOf("bta");

        // Assert
        Assert.Equal(Take, index);
    }

    [Fact]
    public void IndexOf_CodeNotInTheRoster_ReturnsNull()
    {
        // Arrange
        RosterAt(Table, lastIndex: Take);
        RecordAt(Table, Take, "bta", "TAKEMIKAZUCHI");
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        int? index = roster.IndexOf("mko");

        // Assert
        Assert.Null(index);
    }

    [Fact]
    public void IndexOf_RosterNotFound_ReturnsNull()
    {
        // Arrange
        CharacterRoster roster = CreateCharacterRoster(getter: null);

        // Act
        int? index = roster.IndexOf("bta");

        // Assert
        Assert.Null(index);
    }

    [Fact]
    public void EnsureSpriteFolder_RecordWithAnotherFolder_WritesTheFolder()
    {
        // Arrange
        RosterAt(Table, lastIndex: Take);
        RecordAt(Table, Take, "bta", "TAKEMIKAZUCHI");
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        bool changed = roster.EnsureSpriteFolder(Take, SpriteFolder.Char2x);

        // Assert
        Assert.True(changed);
        Assert.Equal(2, _memoryMock.Int32From(Table + Take * RecordLength + SpriteFolderOffset));
    }

    [Fact]
    public void EnsureSpriteFolder_RecordAlreadyUsingTheFolder_ChangesNothing()
    {
        // Arrange
        RosterAt(Table, lastIndex: Take);
        RecordAt(Table, Take, "bta", "TAKEMIKAZUCHI");
        _memoryMock.Int32At(Table + Take * RecordLength + SpriteFolderOffset, 2);
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        bool changed = roster.EnsureSpriteFolder(Take, SpriteFolder.Char2x);

        // Assert
        Assert.False(changed);
    }

    [Fact]
    public void EnsureSpriteFolder_RosterNotFound_ChangesNothing()
    {
        // Arrange
        CharacterRoster roster = CreateCharacterRoster(getter: null);

        // Act
        bool changed = roster.EnsureSpriteFolder(Take, SpriteFolder.Char2x);

        // Assert
        Assert.False(changed);
    }

    [Fact]
    public void SpriteFolderOf_RecordUsingTheTwiceSizeFolder_ReturnsIt()
    {
        // Arrange
        RosterAt(Table, lastIndex: Kohaku);
        RecordAt(Table, Kohaku, "mko", "M_KOHAKU");
        _memoryMock.Int32At(Table + Kohaku * RecordLength + SpriteFolderOffset, 2);
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        SpriteFolder? folder = roster.SpriteFolderOf(Kohaku);

        // Assert
        Assert.Equal(SpriteFolder.Char2x, folder);
    }

    [Fact]
    public void SpriteFolderOf_RecordWithAFolderTheGameDoesNotHave_ReturnsNull()
    {
        // Arrange
        RosterAt(Table, lastIndex: Kohaku);
        RecordAt(Table, Kohaku, "mko", "M_KOHAKU");
        _memoryMock.Int32At(Table + Kohaku * RecordLength + SpriteFolderOffset, 7);
        CharacterRoster roster = CreateCharacterRoster();

        // Act
        SpriteFolder? folder = roster.SpriteFolderOf(Kohaku);

        // Assert
        Assert.Null(folder);
    }

    [Fact]
    public void SpriteFolderOf_RosterNotFound_ReturnsNull()
    {
        // Arrange
        CharacterRoster roster = CreateCharacterRoster(getter: null);

        // Act
        SpriteFolder? folder = roster.SpriteFolderOf(Kohaku);

        // Assert
        Assert.Null(folder);
    }

    private void RosterAt(uint table)
    {
        _memoryMock.Map(Getter, 8).BytesAt(Getter, [0xB8, 0, 0, 0, 0, GameCode.Return]).UInt32At(Getter + 1, table);
    }

    private void RosterAt(uint table, int lastIndex)
    {
        RosterAt(table);
        _memoryMock.Map(table + LastIndexOffset, sizeof(int)).Int32At(table + LastIndexOffset, lastIndex);
    }

    private void RecordAt(uint table, int index, string code, string shortName)
    {
        uint record = table + (uint)index * RecordLength;
        _memoryMock.Map(record, (int)RecordLength);
        _memoryMock.BytesAt(record, Encoding.Latin1.GetBytes(code));
        _memoryMock.BytesAt(record + ShortNameOffset, Encoding.Latin1.GetBytes(shortName));
    }

    private static GameCode CreateRosterCode() =>
        new GameCode(CodeSize)
            .Bytes(0x000, "33 FF ?? ?? ?? ?? ?? 8B 80 00 2E 00 00 83 C0 01 0F 84")
            .CallAt(0x002, GetterOffset)
            .Bytes(GetterOffset, "B8 68 77 C3 00 C3");
}
