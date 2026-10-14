using BBTAG_IM.Core.Scripting.Dev;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class ScriptEditorProgramTests
{
    private const string Script = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.py";

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
}
