using BBTAG_IM.Core.Scripting.Dev;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class ScriptEditorProgramTests
{
    private const string Script = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.py";
    private const string CSharpScript = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.cs";
    private const string Solution = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\bbscript\Scripts\BBScript.slnx";

    [Fact]
    public void Arguments_VisualStudio_OpensTheFileInTheRunningInstance()
    {
        // Arrange
        string program = @"C:\Program Files\Microsoft Visual Studio\18\Enterprise\Common7\IDE\devenv.exe";

        // Act
        string arguments = ScriptEditorProgram.Arguments(program, Script, 12);

        // Assert
        Assert.Equal($"/edit \"{Script}\"", arguments);
    }

    [Fact]
    public void Arguments_VisualStudioCode_GoesToTheLine()
    {
        // Arrange
        string program = @"C:\Users\Player\AppData\Local\Programs\Microsoft VS Code\Code.exe";

        // Act
        string arguments = ScriptEditorProgram.Arguments(program, Script, 12);

        // Assert
        Assert.Equal($"-g \"{Script}:12\"", arguments);
    }

    [Fact]
    public void Arguments_ProgramNameInAnotherCase_IsStillRecognized()
    {
        // Arrange
        string program = @"C:\VS\DEVENV.EXE";

        // Act
        string arguments = ScriptEditorProgram.Arguments(program, Script, 12);

        // Assert
        Assert.Equal($"/edit \"{Script}\"", arguments);
    }

    [Fact]
    public void Arguments_OtherProgram_PassesOnlyTheQuotedFile()
    {
        // Arrange
        string program = @"C:\Program Files (x86)\Notepad++\notepad++.exe";

        // Act
        string arguments = ScriptEditorProgram.Arguments(program, Script, 12);

        // Assert
        Assert.Equal($"\"{Script}\"", arguments);
    }

    [Fact]
    public void Arguments_CSharpScriptInVisualStudio_OpensOnlyTheSolution()
    {
        // Arrange
        string program = @"C:\VS\devenv.exe";

        // Act
        string arguments = ScriptEditorProgram.Arguments(program, CSharpScript, 12, Solution);

        // Assert
        Assert.Equal($"\"{Solution}\"", arguments);
    }

    [Theory]
    [InlineData("!VisualStudio.DTE.18.0:45964")]
    [InlineData("!VisualStudio.DTE.17.0:1200")]
    public void IsAutomationName_VisualStudioInstance_ReturnsTrue(string displayName)
    {
        // Arrange
        string name = displayName;

        // Act
        bool automation = ScriptEditorProgram.IsAutomationName(name);

        // Assert
        Assert.True(automation);
    }

    [Theory]
    [InlineData("!VisualStudio.Solution.18.0:45964")]
    [InlineData("!{00020905-0000-0000-C000-000000000046}")]
    [InlineData("")]
    public void IsAutomationName_OtherRunningObject_ReturnsFalse(string displayName)
    {
        // Arrange
        string name = displayName;

        // Act
        bool automation = ScriptEditorProgram.IsAutomationName(name);

        // Assert
        Assert.False(automation);
    }

    [Fact]
    public void IsSolution_SamePathInAnotherCase_ReturnsTrue()
    {
        // Arrange
        string loaded = Solution.ToUpperInvariant();

        // Act
        bool same = ScriptEditorProgram.IsSolution(loaded, Solution);

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void IsSolution_PathWithDotSegments_ReturnsTrue()
    {
        // Arrange
        string loaded = @"D:\BBTAG\mods\DEV\..\BBTAG-Improvement-Mod\bbscript\Scripts\BBScript.slnx";

        // Act
        bool same = ScriptEditorProgram.IsSolution(loaded, Solution);

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void IsSolution_AnotherSolution_ReturnsFalse()
    {
        // Arrange
        string loaded = @"D:\Work\Syscambio\Syscambio.sln";

        // Act
        bool same = ScriptEditorProgram.IsSolution(loaded, Solution);

        // Assert
        Assert.False(same);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsSolution_NoSolutionLoaded_ReturnsFalse(string? loaded)
    {
        // Arrange
        string? fullName = loaded;

        // Act
        bool same = ScriptEditorProgram.IsSolution(fullName, Solution);

        // Assert
        Assert.False(same);
    }

    [Fact]
    public void Arguments_CSharpScriptInVisualStudioCode_OpensTheFolderAndGoesToTheLine()
    {
        // Arrange
        string program = @"C:\Code\Code.exe";

        // Act
        string arguments = ScriptEditorProgram.Arguments(program, CSharpScript, 12, Solution);

        // Assert
        Assert.Equal($"\"{Path.GetDirectoryName(Solution)}\" -g \"{CSharpScript}:12\"", arguments);
    }
}
