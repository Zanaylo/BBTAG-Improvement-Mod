using BBTAG_IM.Core.Harness;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Harness;

public class HarnessCommandsTests
{
    private readonly IHarnessTarget _targetMock = Substitute.For<IHarnessTarget>();

    [Fact]
    public void Execute_Ping_RepliesWithTheProcess()
    {
        // Arrange
        _targetMock.ProcessId.Returns(4321);

        // Act
        string reply = HarnessCommands.Execute("ping", _targetMock);

        // Assert
        Assert.Equal("ok 4321", reply);
    }

    [Fact]
    public void Execute_Keys_PlaysTheParsedScript()
    {
        // Arrange
        _targetMock.PlayKeys(Arg.Any<KeyScript>()).Returns(true);

        // Act
        string reply = HarnessCommands.Execute("keys hold=2 NUMPAD7,A", _targetMock);

        // Assert
        Assert.Equal("ok 2", reply);
        _targetMock.Received(1).PlayKeys(Arg.Is<KeyScript>(script => script.Steps.Count == 2 && script.Steps[0].HoldFrames == 2));
    }

    [Fact]
    public void Execute_KeysTheGameDidNotTake_RepliesWithAnError()
    {
        // Arrange
        _targetMock.PlayKeys(Arg.Any<KeyScript>()).Returns(false);

        // Act
        string reply = HarnessCommands.Execute("keys A", _targetMock);

        // Assert
        Assert.Equal("error the game stopped taking frames before the keys were played", reply);
    }

    [Fact]
    public void Execute_KeysWithAnUnknownKey_RepliesWithTheParserError()
    {
        // Act
        string reply = HarnessCommands.Execute("keys NOPE", _targetMock);

        // Assert
        Assert.Equal("error unknown key NOPE", reply);
    }

    [Fact]
    public void Execute_Grab_PassesTheWholePath()
    {
        // Arrange
        _targetMock.Grab(@"C:\temp\my shot.png").Returns("ok 1920x1080");

        // Act
        string reply = HarnessCommands.Execute(@"grab C:\temp\my shot.png", _targetMock);

        // Assert
        Assert.Equal("ok 1920x1080", reply);
    }

    [Fact]
    public void Execute_GrabWithoutPath_RepliesWithAnError()
    {
        // Act
        string reply = HarnessCommands.Execute("grab", _targetMock);

        // Assert
        Assert.Equal("error grab needs a path", reply);
    }

    [Theory]
    [InlineData("menukey 17 down", 17, true)]
    [InlineData("menukey 16 up", 16, false)]
    public void Execute_MenuKey_PassesTheKeyAndItsState(string command, int key, bool down)
    {
        // Arrange
        _targetMock.MenuKey(key, down).Returns("ok key");

        // Act
        string reply = HarnessCommands.Execute(command, _targetMock);

        // Assert
        Assert.Equal("ok key", reply);
    }

    [Theory]
    [InlineData("menukey")]
    [InlineData("menukey 17")]
    [InlineData("menukey ctrl down")]
    [InlineData("menukey 17 sideways")]
    public void Execute_MenuKeyWithWrongWords_RepliesWithAnError(string command)
    {
        // Act
        string reply = HarnessCommands.Execute(command, _targetMock);

        // Assert
        Assert.Equal("error menukey needs a key number and down or up", reply);
    }

    [Fact]
    public void Execute_MenuClick_PassesThePoint()
    {
        // Arrange
        _targetMock.MenuClick(120, 340).Returns("ok click");

        // Act
        string reply = HarnessCommands.Execute("menuclick 120 340", _targetMock);

        // Assert
        Assert.Equal("ok click", reply);
    }

    [Theory]
    [InlineData("menuclick")]
    [InlineData("menuclick 120")]
    [InlineData("menuclick left top")]
    public void Execute_MenuClickWithWrongWords_RepliesWithAnError(string command)
    {
        // Act
        string reply = HarnessCommands.Execute(command, _targetMock);

        // Assert
        Assert.Equal("error menuclick needs two numbers", reply);
    }

    [Theory]
    [InlineData("state")]
    [InlineData("menu")]
    [InlineData("back")]
    [InlineData("quit")]
    public void Execute_SimpleCommand_RepliesWithTheTargetAnswer(string command)
    {
        // Arrange
        _targetMock.State().Returns("ok state");
        _targetMock.ToggleMenu().Returns("ok menu");
        _targetMock.BackToCharacterSelect().Returns("ok back");
        _targetMock.Quit().Returns("ok quit");

        // Act
        string reply = HarnessCommands.Execute(command, _targetMock);

        // Assert
        Assert.Equal($"ok {command}", reply);
    }

    [Fact]
    public void Execute_UnknownCommand_RepliesWithAnError()
    {
        // Act
        string reply = HarnessCommands.Execute("dance", _targetMock);

        // Assert
        Assert.Equal("error unknown command dance", reply);
    }

    [Fact]
    public void Execute_EmptyLine_RepliesWithAnError()
    {
        // Act
        string reply = HarnessCommands.Execute("   ", _targetMock);

        // Assert
        Assert.Equal("error empty command", reply);
    }
}
