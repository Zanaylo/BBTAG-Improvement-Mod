using BBTAG_IM.Core.Porting;

namespace BBTAG_IM.Core.Tests.Porting;

public class SpriteRemapTests
{
    private const string SpritePrefix = "Action_";

    private static readonly Dictionary<int, int[]> Target = new()
    {
        [0] = [9, 8],
        [1] = [3, 4, 5, 6],
    };

    [Fact]
    public void Apply_ActionSprite_TakesTheDurationOfTheTargetFrame()
    {
        // Arrange
        string script = "def A():\n    sprite('Action_000_01', 7)\n";

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: true, SpritePrefix).Apply(script);

        // Assert
        Assert.Equal("def A():\n    sprite('Action_000_01', 8)\n", result);
    }

    [Fact]
    public void Apply_PatternWithAnAlias_ShowsTheAliasEvenWhenTheNumberExists()
    {
        // Arrange
        string script = "def A():\n    sprite('Action_001_00', 7)\n";
        Dictionary<int, int> aliases = new() { [1] = 0 };

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: false, "Port_", aliases).Apply(script);

        // Assert
        Assert.Contains("sprite('Port_000_00', 9)", result);
    }

    [Fact]
    public void Apply_HitSprite_FollowsTheSameRules()
    {
        // Arrange
        string script = "def A():\n    HitSprites(2, 'Action_001_01')\n    HitSprites(3, 'Action_009_00')\n";
        Dictionary<int, int> aliases = new() { [1] = 0 };

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: false, "Port_", aliases).Apply(script);

        // Assert
        Assert.Contains("HitSprites(2, 'Port_000_01')", result);
        Assert.Contains("HitSprites(3, 'Port_000_00')", result);
    }

    [Fact]
    public void Apply_TargetHasMoreFrames_AddsThemAfterTheLastUseInTheFunction()
    {
        // Arrange
        string script = "def A():\n    sprite('Action_001_00', 1)\n    sprite('Action_001_01', 1)\n    Recovery()\n";

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: true, SpritePrefix).Apply(script);

        // Assert
        Assert.Equal(
            "def A():\n    sprite('Action_001_00', 3)\n    sprite('Action_001_01', 4)\n    sprite('Action_001_02', 5)\n    sprite('Action_001_03', 6)\n    Recovery()\n",
            result);
    }

    [Fact]
    public void Apply_TargetHasFewerFrames_HoldsItsLastFrameAndKeepsTheCommands()
    {
        // Arrange
        string script = "def A():\n    sprite('Action_000_01', 1)\n    sprite('Action_000_05', 1)\n    Recovery()\n";

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: true, SpritePrefix).Apply(script);

        // Assert
        Assert.Equal("def A():\n    sprite('Action_000_01', 8)\n    sprite('Action_000_01', 8)\n    Recovery()\n", result);
    }

    [Fact]
    public void Apply_PatternTheTargetLacks_FallsBackToTheFirstFrame()
    {
        // Arrange
        string script = "def A():\n    sprite('Action_300_02', 4)\n";

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: true, SpritePrefix).Apply(script);

        // Assert
        Assert.Equal("def A():\n    sprite('Action_000_00', 4)\n", result);
    }

    [Fact]
    public void Apply_TwoFunctions_TreatsEachSeparately()
    {
        // Arrange
        string script = "def A():\n    sprite('Action_000_00', 1)\ndef B():\n    sprite('Action_000_00', 1)\n";

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: true, SpritePrefix).Apply(script);

        // Assert
        Assert.Equal(
            "def A():\n    sprite('Action_000_00', 9)\n    sprite('Action_000_01', 8)\ndef B():\n    sprite('Action_000_00', 9)\n    sprite('Action_000_01', 8)\n",
            result);
    }

    [Fact]
    public void Apply_OtherSpriteKept_StaysAsItIs()
    {
        // Arrange
        string script = "def A():\n    sprite('Hyd123_01', 4)\n    sprite('null', 2)\n";

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: true, SpritePrefix).Apply(script);

        // Assert
        Assert.Equal(script, result);
    }

    [Fact]
    public void Apply_OtherSpriteNotKept_FallsBackToTheFirstFrame()
    {
        // Arrange
        string script = "def A():\n    sprite('Hyd123_01', 4)\n";

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: false, SpritePrefix).Apply(script);

        // Assert
        Assert.Equal("def A():\n    sprite('Action_000_00', 4)\n", result);
    }

    [Fact]
    public void Apply_OtherPrefix_RenamesEverySpriteItTouches()
    {
        // Arrange
        string script = "def A():\n    sprite('Action_000_00', 1)\n    sprite('Action_300_00', 2)\n";

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: false, "Port_").Apply(script);

        // Assert
        Assert.Equal("def A():\n    sprite('Port_000_00', 9)\n    sprite('Port_000_01', 8)\n    sprite('Port_000_00', 2)\n", result);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("keep")]
    public void Apply_MarkerSpriteNotKept_StaysBecauseTheEngineReadsIt(string marker)
    {
        // Arrange
        string script = $"def A():\n    sprite('{marker}', 4)\n";

        // Act
        string result = new SpriteRemap(Target, keepOtherSprites: false, SpritePrefix).Apply(script);

        // Assert
        Assert.Equal(script, result);
    }
}
