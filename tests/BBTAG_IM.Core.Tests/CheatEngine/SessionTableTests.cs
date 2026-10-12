using System.Xml.Linq;
using BBTAG_IM.Core.CheatEngine;

namespace BBTAG_IM.Core.Tests.CheatEngine;

public class SessionTableTests
{
    private const string Lua = "if a < b and c then return 1 end";

    [Fact]
    public void Embed_TableWithLuaScript_AppendsTheEscapedLuaKeepingTheTableNewLines()
    {
        // Arrange
        string table = "<CheatTable>\r\n  <CheatEntries/>\r\n  <LuaScript>print(1)\r\n</LuaScript>\r\n</CheatTable>\r\n";

        // Act
        string session = SessionTable.Embed(table, Lua);

        // Assert
        Assert.Equal(
            "<CheatTable>\r\n  <CheatEntries/>\r\n  <LuaScript>print(1)\r\nif a &lt; b and c then return 1 end\r\n</LuaScript>\r\n</CheatTable>\r\n",
            session);
    }

    [Fact]
    public void Embed_TableWithLuaScript_KeepsTheOriginalScriptBeforeTheBridge()
    {
        // Arrange
        string table = "<CheatTable><CheatEntries/><LuaScript>print(1)\n</LuaScript></CheatTable>";

        // Act
        string session = SessionTable.Embed(table, Lua);

        // Assert
        Assert.Equal("print(1)\n" + Lua + "\n", XDocument.Parse(session).Root!.Element("LuaScript")!.Value);
    }

    [Fact]
    public void Embed_TableWithoutLuaScript_AddsOne()
    {
        // Act
        string session = SessionTable.Embed("<CheatTable><CheatEntries/></CheatTable>", Lua);

        // Assert
        Assert.Equal(Lua + "\n", XDocument.Parse(session).Root!.Element("LuaScript")!.Value);
    }

    [Fact]
    public void Embed_TextThatIsNotACheatTable_ThrowsInvalidSessionTable()
    {
        // Act
        Exception? exception = Record.Exception(() => SessionTable.Embed("<Other/>", Lua));

        // Assert
        Assert.IsType<InvalidSessionTableException>(exception);
    }
}
