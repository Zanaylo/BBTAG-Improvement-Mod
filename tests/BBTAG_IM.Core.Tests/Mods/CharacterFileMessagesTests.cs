using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Tests.Mods;

public class CharacterFileMessagesTests
{
    [Fact]
    public void From_NoReports_SaysEveryCharacterIsComplete()
    {
        // Act
        IReadOnlyList<string> messages = CharacterFileMessages.From([]);

        // Assert
        Assert.Equal(["Every character in the roster has the files a match needs."], messages);
    }

    [Fact]
    public void From_CharactersWithoutFiles_GroupsThemInOneMessage()
    {
        // Arrange
        CharacterFileReport[] reports = [new("dak", ["a", "b"], 2), new("dkn", ["a", "b"], 2)];

        // Act
        IReadOnlyList<string> messages = CharacterFileMessages.From(reports);

        // Assert
        Assert.Equal(["2 characters in the roster have no files at all. Picking one will crash the game: dak, dkn."], messages);
    }

    [Fact]
    public void From_OneCharacterWithoutFiles_UsesTheSingular()
    {
        // Arrange
        CharacterFileReport[] reports = [new("dak", ["a", "b"], 2)];

        // Act
        IReadOnlyList<string> messages = CharacterFileMessages.From(reports);

        // Assert
        Assert.Equal(["1 character in the roster has no files at all. Picking it will crash the game: dak."], messages);
    }

    [Fact]
    public void From_IncompleteCharacter_UsesItsOwnMessage()
    {
        // Arrange
        CharacterFileReport report = new("mko", ["a"], 2);

        // Act
        IReadOnlyList<string> messages = CharacterFileMessages.From([report]);

        // Assert
        Assert.Equal([report.Message], messages);
    }

    [Fact]
    public void From_BothKinds_PutsTheIncompleteCharactersFirst()
    {
        // Arrange
        CharacterFileReport incomplete = new("mko", ["a"], 2);
        CharacterFileReport absent = new("dak", ["a", "b"], 2);

        // Act
        IReadOnlyList<string> messages = CharacterFileMessages.From([absent, incomplete]);

        // Assert
        Assert.Equal(2, messages.Count);
        Assert.Equal(incomplete.Message, messages[0]);
    }
}
