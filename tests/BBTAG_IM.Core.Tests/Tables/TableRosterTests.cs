using BBTAG_IM.Core.Tables;

namespace BBTAG_IM.Core.Tests.Tables;

public class TableRosterTests
{
    [Fact]
    public void Codes_CharactersScript_ReturnsEveryIdInOrder()
    {
        // Arrange
        string xml = Table(Entry("Characters", """
            local characters = {
                { ID = "brg", ShortName = "B_RAGNA" },
                { ID = "mko", ShortName = "M_KOHAKU" },
            }
            """));

        // Act
        IReadOnlyList<string> codes = TableRoster.Codes(xml);

        // Assert
        Assert.Equal(["brg", "mko"], codes);
    }

    [Fact]
    public void Codes_PlaceholderEntry_LeavesItOut()
    {
        // Arrange
        string xml = Table(Entry("Characters", """{ ID = "brg" }, { ID = "temp" }"""));

        // Act
        IReadOnlyList<string> codes = TableRoster.Codes(xml);

        // Assert
        Assert.Equal(["brg"], codes);
    }

    [Fact]
    public void Codes_CharactersScriptInsideGroups_FindsIt()
    {
        // Arrange
        string xml = Table($"""
            <CheatEntry><ID>1</ID><Description>"Setup"</Description><CheatEntries>{Entry("Characters", """{ ID = "mko" }""")}</CheatEntries></CheatEntry>
            """);

        // Act
        IReadOnlyList<string> codes = TableRoster.Codes(xml);

        // Assert
        Assert.Equal(["mko"], codes);
    }

    [Fact]
    public void Codes_IdInAnotherScript_LeavesItOut()
    {
        // Arrange
        string xml = Table(Entry("Songs", """{ ID = "mko" }"""));

        // Act
        IReadOnlyList<string> codes = TableRoster.Codes(xml);

        // Assert
        Assert.Empty(codes);
    }

    [Fact]
    public void Codes_NotXml_ThrowsInvalidCheatTableException()
    {
        // Act
        Exception? exception = Record.Exception(() => TableRoster.Codes("not a table"));

        // Assert
        Assert.IsType<InvalidCheatTableException>(exception);
    }

    private static string Table(string entries) => $"<CheatTable><CheatEntries>{entries}</CheatEntries></CheatTable>";

    private static string Entry(string description, string script) =>
        $"<CheatEntry><ID>2</ID><Description>\"{description}\"</Description><AssemblerScript>{script}</AssemblerScript></CheatEntry>";
}
