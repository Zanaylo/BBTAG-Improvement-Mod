using BBTAG_IM.Core.Harness;

namespace BBTAG_IM.Core.Tests.Harness;

public class KeyScriptTests
{
    [Theory]
    [InlineData("A", 0x41)]
    [InlineData("x", 0x58)]
    [InlineData("7", 0x37)]
    [InlineData("NUMPAD7", 0x67)]
    [InlineData("numpad0", 0x60)]
    [InlineData("UP", 0x26)]
    [InlineData("LEFT", 0x25)]
    [InlineData("ENTER", 0x0D)]
    [InlineData("ESCAPE", 0x1B)]
    [InlineData("SPACE", 0x20)]
    [InlineData("F1", 0x70)]
    [InlineData("F12", 0x7B)]
    [InlineData("0x67", 0x67)]
    public void Key_Name_ReturnsItsVirtualKey(string name, int expected)
    {
        // Act
        int? key = HarnessKeys.Key(name);

        // Assert
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData("NOPE")]
    [InlineData("0x1FF")]
    [InlineData("")]
    public void Key_UnknownName_ReturnsNull(string name)
    {
        // Act
        int? key = HarnessKeys.Key(name);

        // Assert
        Assert.Null(key);
    }

    [Fact]
    public void Parse_StepsWithOptions_UsesTheHoldAndGapForEveryStep()
    {
        // Act
        KeyScript script = KeyScript.Parse(["hold=2", "gap=6", "NUMPAD7,A"]);

        // Assert
        Assert.Equal([new KeyStep([0x67], 2, 6), new KeyStep([0x41], 2, 6)], script.Steps);
    }

    [Fact]
    public void Parse_ChordWithOwnHold_HoldsTheKeysTogether()
    {
        // Act
        KeyScript script = KeyScript.Parse(["S+D*10"]);

        // Assert
        KeyStep step = Assert.Single(script.Steps);
        Assert.Equal([0x53, 0x44], step.Keys);
        Assert.Equal(10, step.HoldFrames);
    }

    [Fact]
    public void Parse_NoOptions_UsesTheDefaults()
    {
        // Act
        KeyScript script = KeyScript.Parse(["A"]);

        // Assert
        Assert.Equal(new KeyStep([0x41], KeyScript.DefaultHoldFrames, KeyScript.DefaultGapFrames), Assert.Single(script.Steps));
    }

    [Fact]
    public void Parse_UnknownKey_ThrowsWithItsName()
    {
        // Act
        Exception? exception = Record.Exception(() => KeyScript.Parse(["A,NOPE"]));

        // Assert
        Assert.Equal("unknown key NOPE", Assert.IsType<InvalidKeyScriptException>(exception).Message);
    }

    [Fact]
    public void Parse_NoKeys_Throws()
    {
        // Act
        Exception? exception = Record.Exception(() => KeyScript.Parse(["hold=2"]));

        // Assert
        Assert.IsType<InvalidKeyScriptException>(exception);
    }

    [Fact]
    public void Frames_Steps_AddsHoldAndGapOfEveryStep()
    {
        // Arrange
        KeyScript script = KeyScript.Parse(["hold=2", "gap=3", "A,B*5"]);

        // Act
        int frames = script.Frames;

        // Assert
        Assert.Equal(13, frames);
    }
}
