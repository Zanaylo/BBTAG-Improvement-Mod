using BBTAG_IM.Core.Scripting.Compiling;

namespace BBTAG_IM.Core.Tests.Scripting.Compiling;

public class CompilerMessagesTests
{
    private const string Script = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.cs";

    [Fact]
    public void Error_Mistake_WritesTheLineVisualStudioListsAsAnError()
    {
        // Arrange
        ScriptCompileException mistake = new("Damag is not a known command.", 25);

        // Act
        string message = CompilerMessages.Error(Script, mistake);

        // Assert
        Assert.Equal($"{Script}(25): error BBS0001: Damag is not a known command.", message);
    }
}
