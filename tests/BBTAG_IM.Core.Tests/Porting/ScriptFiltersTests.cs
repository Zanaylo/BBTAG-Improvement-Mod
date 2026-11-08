using BBTAG_IM.Core.Porting;

namespace BBTAG_IM.Core.Tests.Porting;

public class ScriptFiltersTests
{
    [Fact]
    public void DropSpecialMoves_SpecialMove_RemovesItsRegistration()
    {
        // Arrange
        string script = """
            def MatchInit():
                AirDashFSpeed(23000)

                @StateRegister
                def NmlAtk5A(INPUT_5A):
                    MoveMaxChainRepeat(1)

                @StateRegister
                def ShotA(INPUT_SPECIALMOVE):
                    Move_Input_(INPUT_236)
                    Move_Input_(INPUT_PRESS_A)

                @StateRegister
                def NmlAtk5B(INPUT_5B):
                    MoveMaxChainRepeat(1)
            """;

        // Act
        string result = ScriptFilters.DropSpecialMoves(script);

        // Assert
        Assert.DoesNotContain("ShotA", result);
        Assert.DoesNotContain("INPUT_236", result);
        Assert.Contains("def NmlAtk5A(INPUT_5A):", result);
        Assert.Contains("def NmlAtk5B(INPUT_5B):", result);
    }

    [Theory]
    [InlineData("INPUT_DISTORTION")]
    [InlineData("INPUT_ASTRAL")]
    public void DropSpecialMoves_SuperMove_RemovesItsRegistration(string input)
    {
        // Arrange
        string script = $"    @StateRegister\n    def Super({input}):\n        SkillType(6)\n";

        // Act
        string result = ScriptFilters.DropSpecialMoves(script);

        // Assert
        Assert.DoesNotContain("Super", result);
    }

    [Theory]
    [InlineData("Voiceline('uhy000')")]
    [InlineData("RandomCommonVoiceline(0)")]
    [InlineData("PrivateSE('SE_InsulatorSwingA')")]
    [InlineData("GFX_0('EffNmlAtk5ABlade', 100)")]
    [InlineData("GFX_1('Eff', 0)")]
    public void Silence_TemplateCommand_BecomesPass(string command)
    {
        // Arrange
        string script = $"    sprite('A', 1)\n    {command}\n";

        // Act
        string result = ScriptFilters.Silence(script);

        // Assert
        Assert.Equal("    sprite('A', 1)\n    pass\n", result);
    }

    [Fact]
    public void Silence_OtherCommand_StaysAsItIs()
    {
        // Arrange
        string script = "    Recovery()\n";

        // Act
        string result = ScriptFilters.Silence(script);

        // Assert
        Assert.Equal(script, result);
    }
}
