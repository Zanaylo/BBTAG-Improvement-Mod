using BBTAG_IM.Core.Scripting.EditorHelp;

namespace BBTAG_IM.Core.Tests.Scripting.EditorHelp;

public class ScriptDocsImportTests
{
    private static readonly string[] CommandHeader = ["ID", "Format", "Function", "Notes"];
    private static readonly string[] TagCommandHeader = ["NOTE", "ID", "Format", "Function", "Notes"];
    private static readonly string[] ValueHeader = ["ID", "Operation", "Notes"];

    [Fact]
    public void FromWorkbook_CommandRow_ReadsFormatFunctionAndNotes()
    {
        // Arrange
        var sheets = Sheets(("Command List", [CommandHeader, ["12045.0", "(x, x)", "Sets the multipliers of Ragna's Soul Eater.", "Per hit."]]));

        // Act
        ScriptDocs docs = ScriptDocsImport.FromWorkbook(sheets);

        // Assert
        Assert.Equal(new CommandDoc("Sets the multipliers of Ragna's Soul Eater.", "(x, x)", "Per hit."), docs.Command(12045));
    }

    [Fact]
    public void FromWorkbook_CommandInTheBBCFAndTheTagSheets_UsesTheTagSheet()
    {
        // Arrange
        var sheets = Sheets(
            ("Command List", [CommandHeader, ["2067", "(x, x)", "Barrier gauge to gain.", ""]]),
            ("Command List (TAG)", [TagCommandHeader, ["", "69 (TAG)", "(str)", "Checks the partner.", ""], ["", "2067 (TAG)", "(x, x)", "Partner meter to gain.", ""]]));

        // Act
        ScriptDocs docs = ScriptDocsImport.FromWorkbook(sheets);

        // Assert
        Assert.Equal("Partner meter to gain.", docs.Command(2067)?.Summary);
        Assert.Equal("Checks the partner.", docs.Command(69)?.Summary);
    }

    [Fact]
    public void FromWorkbook_BBCFOnlyRowInTheTagSheet_DoesNotReplaceTheTagRow()
    {
        // Arrange
        var sheets = Sheets(("Command List (TAG)", [TagCommandHeader, ["", "2067 (TAG)", "(x, x)", "Partner meter to gain.", ""], ["", "2067 (CF)", "(x, x)", "Barrier gauge to gain.", ""]]));

        // Act
        ScriptDocs docs = ScriptDocsImport.FromWorkbook(sheets);

        // Assert
        Assert.Equal("Partner meter to gain.", docs.Command(2067)?.Summary);
    }

    [Fact]
    public void FromWorkbook_RowWithoutANumber_IsLeftOut()
    {
        // Arrange
        var sheets = Sheets(("Command List (TAG)", [TagCommandHeader, ["", "*HitstunAnimation(x)", "Effect of parameter used.", "", ""]]));

        // Act
        ScriptDocs docs = ScriptDocsImport.FromWorkbook(sheets);

        // Assert
        Assert.Empty(docs.Commands);
    }

    [Fact]
    public void FromWorkbook_SlotSheets_PreferTheTagSheet()
    {
        // Arrange
        var sheets = Sheets(
            ("SLOT", [ValueHeader, ["4", "IsRunning", ""], ["8", "XVelocity", ""]]),
            ("SLOT (TAG)", [ValueHeader, ["4", "Running in TAG", "Seen in dashes."]]));

        // Act
        ScriptDocs docs = ScriptDocsImport.FromWorkbook(sheets);

        // Assert
        Assert.Equal("Running in TAG. Seen in dashes.", docs.Slot(4));
        Assert.Equal("XVelocity", docs.Slot(8));
    }

    [Fact]
    public void FromWorkbook_EventsSheet_ReadsUponsWithoutTodos()
    {
        // Arrange
        var sheets = Sheets(("Events (ID 15, 17, 28, 29)", [["ID", "Operation"], ["2", "On landing"], ["6", "[TODO]"]]));

        // Act
        ScriptDocs docs = ScriptDocsImport.FromWorkbook(sheets);

        // Assert
        Assert.Equal("On landing", docs.Upon(2));
        Assert.Null(docs.Upon(6));
    }

    private static Dictionary<string, IReadOnlyList<IReadOnlyList<string>>> Sheets(params (string Name, string[][] Rows)[] sheets) =>
        sheets.ToDictionary(sheet => sheet.Name, sheet => (IReadOnlyList<IReadOnlyList<string>>)sheet.Rows);
}
