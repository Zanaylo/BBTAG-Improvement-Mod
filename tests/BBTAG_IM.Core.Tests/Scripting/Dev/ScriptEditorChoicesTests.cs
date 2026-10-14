using BBTAG_IM.Core.Scripting.Dev;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class ScriptEditorChoicesTests
{
    private const string VisualStudioCode = @"C:\Programs\Microsoft VS Code\Code.exe";
    private const string NotepadPlusPlus = @"C:\Program Files (x86)\Notepad++\notepad++.exe";

    [Fact]
    public void Merge_SameProgramTwiceInAnotherCase_KeepsTheFirst()
    {
        // Arrange
        ScriptEditorChoice first = new("Visual Studio Code", VisualStudioCode);
        ScriptEditorChoice again = new("Code", VisualStudioCode.ToUpperInvariant());

        // Act
        IReadOnlyList<ScriptEditorChoice> choices = ScriptEditorChoices.Merge([first, again]);

        // Assert
        Assert.Equal([first], choices);
    }

    [Fact]
    public void Merge_ChoiceWithoutProgram_IsLeftOut()
    {
        // Arrange
        ScriptEditorChoice empty = new("", "");
        ScriptEditorChoice notepad = new("Notepad++", NotepadPlusPlus);

        // Act
        IReadOnlyList<ScriptEditorChoice> choices = ScriptEditorChoices.Merge([empty, notepad]);

        // Assert
        Assert.Equal([notepad], choices);
    }

    [Fact]
    public void Merge_ChoiceWithoutName_IsNamedAfterItsFile()
    {
        // Arrange
        ScriptEditorChoice unnamed = new("", NotepadPlusPlus);

        // Act
        IReadOnlyList<ScriptEditorChoice> choices = ScriptEditorChoices.Merge([unnamed]);

        // Assert
        Assert.Equal([new ScriptEditorChoice("notepad++", NotepadPlusPlus)], choices);
    }

    [Fact]
    public void NameOf_SavedProgramInTheList_ReturnsItsName()
    {
        // Arrange
        IReadOnlyList<ScriptEditorChoice> choices = [new("Visual Studio Code", VisualStudioCode)];

        // Act
        string name = ScriptEditorChoices.NameOf(VisualStudioCode.ToLowerInvariant(), choices);

        // Assert
        Assert.Equal("Visual Studio Code", name);
    }

    [Fact]
    public void NameOf_SavedProgramNotInTheList_ReturnsItsFileName()
    {
        // Arrange
        IReadOnlyList<ScriptEditorChoice> choices = [];

        // Act
        string name = ScriptEditorChoices.NameOf(NotepadPlusPlus, choices);

        // Assert
        Assert.Equal("notepad++", name);
    }
}
