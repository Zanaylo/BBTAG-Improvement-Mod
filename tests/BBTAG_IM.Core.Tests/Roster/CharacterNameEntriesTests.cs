using BBTAG_IM.Core.Roster;

namespace BBTAG_IM.Core.Tests.Roster;

public class CharacterNameEntriesTests
{
    [Fact]
    public void For_Manifest_GivesTheFourNameKeysOfItsUpperCode()
    {
        // Arrange
        CharacterManifest manifest = new("rar", "brg", "M_REDARC", null, Series.Others, new CharacterNames("Red Arcueid", "Red Arc", "RED=ARCUEID"));

        // Act
        IReadOnlyList<KeyValuePair<string, string>> entries = CharacterNameEntries.For(manifest);

        // Assert
        Assert.Equal(
        [
            new("Chara_NameRAR", "Red Arcueid"),
            new("Chara_NameRAR_Short", "Red Arc"),
            new("CharaSelect_NameRAR", "RED=ARCUEID"),
            new("TRIChara_NameRAR_Short", "[Red Arc]"),
        ],
        entries);
    }
}
