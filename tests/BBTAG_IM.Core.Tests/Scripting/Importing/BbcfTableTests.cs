using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Importing;

namespace BBTAG_IM.Core.Tests.Scripting.Importing;

public class BbcfTableTests
{
    [Fact]
    public void Read_Instruction_HasItsNameAndArgumentTypes()
    {
        // Arrange
        string json = """{ "Instructions": { "2": { "Size": 40, "Name": "CellBegin", "Args": [ { "Type": "C32BYTE", "Name": "name" }, { "Type": "S32" } ] } } }""";

        // Act
        BbcfTable table = BbcfTable.Read(json);

        // Assert
        Assert.Equal("CellBegin", table.Commands[2].Name);
        Assert.Equal(["C32BYTE", "S32"], table.Commands[2].ArgumentTypes);
    }

    [Fact]
    public void Read_InstructionWithoutName_IsLeftOut()
    {
        // Arrange
        string json = """{ "Instructions": { "66": { "Size": 8, "Args": [ { "Type": "S32" } ] } } }""";

        // Act
        BbcfTable table = BbcfTable.Read(json);

        // Assert
        Assert.Empty(table.Commands);
    }

    [Fact]
    public void Read_InstructionWithoutArguments_HasNoArgumentTypes()
    {
        // Arrange
        string json = """{ "Instructions": { "1": { "Name": "ActionEnd" } } }""";

        // Act
        BbcfTable table = BbcfTable.Read(json);

        // Assert
        Assert.Empty(table.Commands[1].ArgumentTypes);
    }

    [Fact]
    public void Read_Variable_IsKeyedByItsNumber()
    {
        // Arrange
        string json = """{ "Variables": { "VAR_PlayerVal0": 4 } }""";

        // Act
        BbcfTable table = BbcfTable.Read(json);

        // Assert
        Assert.Equal("VAR_PlayerVal0", table.Variables[4]);
    }

    [Fact]
    public void Read_NotJson_ThrowsInvalidScriptDatabaseException()
    {
        // Arrange
        string json = "{ Instructions";

        // Act
        Exception? exception = Record.Exception(() => BbcfTable.Read(json));

        // Assert
        Assert.IsType<InvalidScriptDatabaseException>(exception);
    }
}
