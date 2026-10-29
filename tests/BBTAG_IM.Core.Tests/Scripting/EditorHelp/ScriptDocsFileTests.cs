using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.EditorHelp;

namespace BBTAG_IM.Core.Tests.Scripting.EditorHelp;

public class ScriptDocsFileTests
{
    [Fact]
    public void Read_WrittenDocs_GivesTheSameDocsBack()
    {
        // Arrange
        ScriptDocs docs = new(
            new Dictionary<int, CommandDoc> { [12045] = new("Soul Eater multipliers.", "(x, x)", "Per \"hit\".") },
            new Dictionary<int, string> { [4] = "IsRunning" },
            new Dictionary<int, string> { [2] = "On landing" });

        // Act
        ScriptDocs read = ScriptDocsFile.Read(ScriptDocsFile.Write(docs));

        // Assert
        Assert.Equal(docs.Command(12045), read.Command(12045));
        Assert.Equal("IsRunning", read.Slot(4));
        Assert.Equal("On landing", read.Upon(2));
    }

    [Fact]
    public void Read_BrokenJson_ThrowsInvalidScriptDatabaseException()
    {
        // Act
        Exception? exception = Record.Exception(() => ScriptDocsFile.Read("{ not json"));

        // Assert
        Assert.IsType<InvalidScriptDatabaseException>(exception);
    }
}
