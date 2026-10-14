using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting;

public class ScriptBinaryTests
{
    [Fact]
    public void Read_ScriptWithTwoStates_ReturnsTheTableAndEachCommand()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().State("A").Command(1).State("B").Command(ScriptTestDatabase.Damage, 5).Command(1).Build();

        // Act
        ScriptBinary script = ScriptBinary.Read(bytes, ScriptTestDatabase.Create());

        // Assert
        Assert.Equal([new ScriptState("A", 0), new ScriptState("B", 40)], script.States);
        Assert.Equal([0, 1, 0, ScriptTestDatabase.Damage, 1], script.Instructions.Select(instruction => instruction.Id));
    }

    [Fact]
    public void Read_CommandMissingFromTheDatabase_ThrowsInvalidScriptException()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().State("A").Command(777).Build();

        // Act
        Exception? exception = Record.Exception(() => ScriptBinary.Read(bytes, ScriptTestDatabase.Create()));

        // Assert
        Assert.IsType<InvalidScriptException>(exception);
    }

    [Fact]
    public void Read_FileEndingInsideACommand_ThrowsInvalidScriptException()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().State("A").Command(ScriptTestDatabase.Damage, 5).Build()[..^2];

        // Act
        Exception? exception = Record.Exception(() => ScriptBinary.Read(bytes, ScriptTestDatabase.Create()));

        // Assert
        Assert.IsType<InvalidScriptException>(exception);
    }

    [Fact]
    public void ReadStates_CountLargerThanTheFile_ThrowsInvalidScriptException()
    {
        // Arrange
        byte[] bytes = [0xFF, 0x00, 0x00, 0x00];

        // Act
        Exception? exception = Record.Exception(() => ScriptBinary.ReadStates(bytes));

        // Assert
        Assert.IsType<InvalidScriptException>(exception);
    }
}
