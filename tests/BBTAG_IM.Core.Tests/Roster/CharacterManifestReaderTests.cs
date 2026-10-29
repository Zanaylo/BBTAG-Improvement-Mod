using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Roster;

namespace BBTAG_IM.Core.Tests.Roster;

public class CharacterManifestReaderTests
{
    private const string Source = @"D:\BBTAG\mods\Red\characters\rar.json";

    [Fact]
    public void Parse_CompleteManifest_ReadsEveryField()
    {
        // Arrange
        string json = """
            {
              "Code": "rar",
              "Template": "brg",
              "ShortName": "M_REDARC",
              "SpriteFolder": "Char2x",
              "Series": "Others",
              "Names": { "Full": "Red Arcueid", "Short": "Red Arc", "Select": "RED=ARCUEID" }
            }
            """;

        // Act
        CharacterManifest manifest = CharacterManifestReader.Parse(json, Source);

        // Assert
        Assert.Equal(
            new CharacterManifest("rar", "brg", "M_REDARC", SpriteFolder.Char2x, Series.Others, new CharacterNames("Red Arcueid", "Red Arc", "RED=ARCUEID")),
            manifest);
    }

    [Fact]
    public void Parse_OnlyRequiredFields_FillsTheDefaults()
    {
        // Arrange
        string json = """{ "Code": "rar", "Template": "brg", "Series": "BlazBlue" }""";

        // Act
        CharacterManifest manifest = CharacterManifestReader.Parse(json, Source);

        // Assert
        Assert.Equal(
            new CharacterManifest("rar", "brg", "RAR", null, Series.BlazBlue, new CharacterNames("RAR", "RAR", "RAR")),
            manifest);
    }

    [Fact]
    public void Parse_UppercaseCode_ThrowsInvalidCharacterManifestException()
    {
        // Arrange
        string json = """{ "Code": "RAR", "Template": "brg", "Series": "BlazBlue" }""";

        // Act
        Exception? exception = Record.Exception(() => CharacterManifestReader.Parse(json, Source));

        // Assert
        Assert.IsType<InvalidCharacterManifestException>(exception);
    }

    [Fact]
    public void Parse_NoTemplate_ThrowsInvalidCharacterManifestException()
    {
        // Arrange
        string json = """{ "Code": "rar", "Series": "BlazBlue" }""";

        // Act
        Exception? exception = Record.Exception(() => CharacterManifestReader.Parse(json, Source));

        // Assert
        Assert.IsType<InvalidCharacterManifestException>(exception);
    }

    [Fact]
    public void Parse_NoSeries_ThrowsInvalidCharacterManifestException()
    {
        // Arrange
        string json = """{ "Code": "rar", "Template": "brg" }""";

        // Act
        Exception? exception = Record.Exception(() => CharacterManifestReader.Parse(json, Source));

        // Assert
        Assert.IsType<InvalidCharacterManifestException>(exception);
    }

    [Fact]
    public void Parse_UnknownSeries_ThrowsInvalidCharacterManifestException()
    {
        // Arrange
        string json = """{ "Code": "rar", "Template": "brg", "Series": "Tekken" }""";

        // Act
        Exception? exception = Record.Exception(() => CharacterManifestReader.Parse(json, Source));

        // Assert
        Assert.IsType<InvalidCharacterManifestException>(exception);
    }

    [Fact]
    public void Parse_BrokenJson_ThrowsInvalidCharacterManifestExceptionNamingTheFile()
    {
        // Act
        Exception? exception = Record.Exception(() => CharacterManifestReader.Parse("{ \"Code\": ", Source));

        // Assert
        InvalidCharacterManifestException invalid = Assert.IsType<InvalidCharacterManifestException>(exception);
        Assert.Contains(Source, invalid.Message);
    }
}
