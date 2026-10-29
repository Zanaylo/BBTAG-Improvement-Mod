using BBTAG_IM.Core.Scripting.EditorHelp;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.EditorHelp;

public class WorkbookTests
{
    [Fact]
    public void Read_TwoSheets_ReturnsEachSheetByName()
    {
        // Arrange
        byte[] file = WorkbookFile.Create(
            ("Command List", [["ID", "Format"], ["30.0", "(x, y)"]]),
            ("SLOT", [["ID", "Operation"], ["4", "IsRunning"]]));

        // Act
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> sheets = Workbook.Read(new MemoryStream(file));

        // Assert
        Assert.Equal(["Command List", "SLOT"], sheets.Keys.Order());
        Assert.Equal(["30.0", "(x, y)"], sheets["Command List"][1]);
        Assert.Equal(["4", "IsRunning"], sheets["SLOT"][1]);
    }

    [Fact]
    public void Read_RowWithAnEmptyCell_KeepsTheColumnsInPlace()
    {
        // Arrange
        byte[] file = WorkbookFile.Create(("TAG", [[null, "69 (TAG)", "(str)"]]));

        // Act
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> sheets = Workbook.Read(new MemoryStream(file));

        // Assert
        Assert.Equal(["", "69 (TAG)", "(str)"], sheets["TAG"][0]);
    }

    [Fact]
    public void Read_NotAWorkbook_ThrowsInvalidDataException()
    {
        // Act
        Exception? exception = Record.Exception(() => Workbook.Read(new MemoryStream([1, 2, 3])));

        // Assert
        Assert.IsType<InvalidDataException>(exception);
    }
}
