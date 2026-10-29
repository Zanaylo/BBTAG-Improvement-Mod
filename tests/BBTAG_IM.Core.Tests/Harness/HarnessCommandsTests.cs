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
    [InlineData("state")]
    [InlineData("back")]
    [InlineData("quit")]
    public void Execute_SimpleCommand_RepliesWithTheTargetAnswer(string command)
    {
        // Arrange
        _targetMock.State().Returns("ok state");
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
