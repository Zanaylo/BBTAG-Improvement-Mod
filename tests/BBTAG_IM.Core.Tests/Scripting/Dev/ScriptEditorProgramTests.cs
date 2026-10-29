using BBTAG_IM.Core.Scripting.Dev;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class ScriptEditorProgramTests
{
    private const string Script = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.py";
    private const string CSharpScript = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.cs";
    private const string Project = @"D:\BBTAG\mods\DEV\Scripts.csproj";

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
    public void Arguments_CSharpScriptInVisualStudio_OpensTheFileInTheRunningInstance()
    {
        // Arrange
        string program = @"C:\VS\devenv.exe";

        // Act
        string arguments = ScriptEditorProgram.Arguments(program, CSharpScript, 12, Project);

        // Assert
        Assert.Equal($"/edit \"{CSharpScript}\"", arguments);
    }

    [Fact]
    public void ProjectArguments_VisualStudioNotRunning_OpensTheProject()
    {
        // Arrange
        string program = @"C:\VS\devenv.exe";

        // Act
        string? arguments = ScriptEditorProgram.ProjectArguments(program, Project, false);

        // Assert
        Assert.Equal($"\"{Project}\"", arguments);
    }

    [Fact]
    public void ProjectArguments_VisualStudioAlreadyRunning_OpensNothingFirst()
    {
        // Arrange
        string program = @"C:\VS\devenv.exe";

        // Act
        string? arguments = ScriptEditorProgram.ProjectArguments(program, Project, true);

        // Assert
        Assert.Null(arguments);
    }

    [Fact]
    public void ProjectArguments_NoProject_OpensNothingFirst()
    {
        // Arrange
        string program = @"C:\VS\devenv.exe";

        // Act
        string? arguments = ScriptEditorProgram.ProjectArguments(program, null, false);

        // Assert
        Assert.Null(arguments);
    }

    [Fact]
    public void ProjectArguments_VisualStudioCode_OpensNothingFirst()
    {
        // Arrange
        string program = @"C:\Code\Code.exe";

        // Act
        string? arguments = ScriptEditorProgram.ProjectArguments(program, Project, false);

        // Assert
        Assert.Null(arguments);
    }

    [Fact]
    public void Arguments_CSharpScriptInVisualStudioCode_OpensTheFolderAndGoesToTheLine()
    {
        // Arrange
        string program = @"C:\Code\Code.exe";

        // Act
        string arguments = ScriptEditorProgram.Arguments(program, CSharpScript, 12, Project);

        // Assert
        Assert.Equal($"\"{Path.GetDirectoryName(Project)}\" -g \"{CSharpScript}:12\"", arguments);
    }
}
