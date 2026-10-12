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
    private const int Ragna = 0;
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

    private void RosterAt(uint table)
    {
        _memoryMock.Map(Getter, 8).BytesAt(Getter, [0xB8, 0, 0, 0, 0, GameCode.Return]).UInt32At(Getter + 1, table);
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
