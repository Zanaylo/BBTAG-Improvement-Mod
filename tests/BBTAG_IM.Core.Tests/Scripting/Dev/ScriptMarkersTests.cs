using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Dev;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class ScriptMarkersTests
{
    [Fact]
    public void Insert_IndentedLine_PutsTheMarkerAboveItWithTheSameIndent()
    {
        // Arrange
        string text = "@State\ndef A():\n    Damage(1)\n";

        // Act
        string marked = ScriptMarkers.Insert(text, 3, "Damag is not a known command.");

        // Assert
        Assert.Equal("@State\ndef A():\n    # !!! Damag is not a known command.\n    Damage(1)\n", marked);
    }

    [Fact]
    public void Insert_WindowsLineEndings_KeepsThem()
    {
        // Arrange
        string text = "@State\r\ndef A():\r\n    pass\r\n";

        // Act
        string marked = ScriptMarkers.Insert(text, 3, "Broke.");

        // Assert
        Assert.Equal("@State\r\ndef A():\r\n    # !!! Broke.\r\n    pass\r\n", marked);
    }

    [Fact]
    public void Insert_LineAfterTheEnd_PutsTheMarkerAtTheTop()
    {
        // Arrange
        string text = "@State\ndef A():\n    pass\n";

        // Act
        string marked = ScriptMarkers.Insert(text, 99, "Broke.");

        // Assert
        Assert.StartsWith("# !!! Broke.\n@State", marked);
    }

    [Fact]
    public void Strip_TextWithMarkers_RemovesOnlyTheMarkerLines()
    {
        // Arrange
        string text = "@State\ndef A():\n    # !!! Broke.\n    # a note\n    pass\n";

        // Act
        string stripped = ScriptMarkers.Strip(text);

        // Assert
        Assert.Equal("@State\ndef A():\n    # a note\n    pass\n", stripped);
    }

    [Fact]
    public void Insert_MessageOverSeveralLines_KeepsItOnOneLine()
    {
        // Arrange
        string text = "@State\ndef A():\n    pass\n";

        // Act
        string marked = ScriptMarkers.Insert(text, 1, "BB_Assert\nline 2");

        // Assert
        Assert.StartsWith("# !!! BB_Assert line 2\n@State", marked);
    }

    [Fact]
    public void Insert_CSharp_UsesACSharpComment()
    {
        // Arrange
        string text = "    {\n        Damag(1);\n    }\n";

        // Act
        string marked = ScriptMarkers.Insert(text, 2, "Damag is not a known command.", ScriptLanguage.CSharp);

        // Assert
        Assert.Equal("    {\n        // !!! Damag is not a known command.\n        Damag(1);\n    }\n", marked);
    }

    [Fact]
    public void Strip_CSharpMarkers_RemovesThem()
    {
        // Arrange
        string text = "    {\n        // !!! Old mistake\n        // a note\n        Damage(1);\n    }\n";

        // Act
        string stripped = ScriptMarkers.Strip(text);

        // Assert
        Assert.Equal("    {\n        // a note\n        Damage(1);\n    }\n", stripped);
    }
}
