namespace BBTAG_IM.Core.CheatEngine;

public static class SessionTable
{
    private const string TableClose = "</CheatTable>";
    private const string LuaScriptOpen = "<LuaScript>";
    private const string LuaScriptClose = "</LuaScript>";
    private const string WindowsNewLine = "\r\n";
    private const string UnixNewLine = "\n";

    public static string Embed(string tableXml, string lua)
    {
        var tableClose = tableXml.LastIndexOf(TableClose, StringComparison.Ordinal);

        if (tableClose < 0)
            throw new InvalidSessionTableException("The file is not a Cheat Engine table.");

        var newLine = tableXml.Contains(WindowsNewLine, StringComparison.Ordinal) ? WindowsNewLine : UnixNewLine;
        var escapedLua = Escape(lua) + newLine;
        var luaClose = tableXml.LastIndexOf(LuaScriptClose, StringComparison.Ordinal);

        if (luaClose < 0)
            return tableXml.Insert(tableClose, LuaScriptOpen + escapedLua + LuaScriptClose);

        var head = tableXml[..luaClose];
        var separator = head.EndsWith(newLine, StringComparison.Ordinal) ? "" : newLine;
        return head + separator + escapedLua + tableXml[luaClose..];
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
