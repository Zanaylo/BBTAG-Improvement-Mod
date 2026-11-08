using BBTAG_IM.Core.Porting.Moves;

namespace BBTAG_IM.Core.Tests.Porting.Moves;

public class MoveTemplateTests
{
    private const string Template = """
        @Subroutine
        def MatchInit():
            AirDashFSpeed(23000)

            @StateRegister
            def NmlAtk5A(INPUT_5A):
                MoveMaxChainRepeat(1)

            @StateRegister
            def ShotA(INPUT_SPECIALMOVE):
                Move_Input_(INPUT_236)

            @StateRegister
            def CmnActInvincibleAttack(INPUT_REVERSAL):
                DamageStunPriority(0)
            TempPriorityMultiplier(NmlAtk5A, ShotA, 10000000)
            TrainingCounterattack(1, NmlAtk5A)


        @State
        def CmnActStand():
            sprite('Action_000_00', 7)


        @State
        def NmlAtk5A():
            sprite('Action_001_00', 4)


        @State
        def ShotA():
            sprite('Action_160_00', 4)
            enterState(ShotB)
        """;

    [Fact]
    public void Replace_RemovesTheMovesOfTheTemplate()
    {
        // Act
        string script = MoveTemplate.Replace(Template, "@StateRegister\ndef Skill_236A(INPUT_SPECIALMOVE):\n    FollowupOnly(1)\n", "@State\ndef Skill_236A():\n    sprite('Port_195_00', 3)\n");

        // Assert
        Assert.DoesNotContain("ShotA", script);
        Assert.DoesNotContain("Action_001_00", script);
        Assert.DoesNotContain("TempPriorityMultiplier", script);
        Assert.DoesNotContain("TrainingCounterattack", script);
    }

    [Fact]
    public void Replace_KeepsTheCommonStates()
    {
        // Act
        string script = MoveTemplate.Replace(Template, "", "");

        // Assert
        Assert.Contains("def CmnActStand():", script);
        Assert.Contains("def CmnActInvincibleAttack(INPUT_REVERSAL):", script);
        Assert.Contains("AirDashFSpeed(23000)", script);
    }

    [Fact]
    public void Replace_AddsTheNewRegistrationsInsideMatchInit()
    {
        // Act
        string script = MoveTemplate.Replace(Template, "@StateRegister\ndef Skill_236A(INPUT_SPECIALMOVE):\n    FollowupOnly(1)\n", "");

        // Assert
        Assert.Contains("    AirDashFSpeed(23000)\n\n    @StateRegister\n    def Skill_236A(INPUT_SPECIALMOVE):\n        FollowupOnly(1)\n", script);
    }

    [Fact]
    public void Replace_CrushTrigger_NoLongerTakesTheCButtonButKeepsItsState()
    {
        // Arrange
        const string crush = """
            @Subroutine
            def MatchInit():

                @StateRegister
                def CmnActCrushAttack(INPUT_CRUSH_TRIGGER):
                    MoveMid()


            @State
            def CmnActCrushAttack():
                sprite('Action_009_00', 4)
            """;

        // Act
        string script = MoveTemplate.Replace(crush, "", "");

        // Assert
        Assert.DoesNotContain("INPUT_CRUSH_TRIGGER", script);
        Assert.Contains("def CmnActCrushAttack():", script);
    }

    [Fact]
    public void Replace_StatsGoInsideMatchInit()
    {
        // Act
        string script = MoveTemplate.Replace(Template, "", "", stats: ["WalkFSpeed(9219)"]);

        // Assert
        Assert.Contains("    WalkFSpeed(9219)", script);
        Assert.True(script.IndexOf("WalkFSpeed", StringComparison.Ordinal) < script.IndexOf("@StateRegister", StringComparison.Ordinal));
    }

    [Fact]
    public void Replace_ReplacedCommonState_LeavesTheTemplate()
    {
        // Arrange
        const string state = "def CmnActStand():";

        // Act
        string script = MoveTemplate.Replace(Template, "", "", ["CmnActStand"]);

        // Assert
        Assert.DoesNotContain(state, script);
    }

    [Fact]
    public void Replace_AddsTheNewStatesAtTheEnd()
    {
        // Act
        string script = MoveTemplate.Replace(Template, "", "@State\ndef Skill_236A():\n    sprite('Port_195_00', 3)\n");

        // Assert
        Assert.EndsWith("def Skill_236A():\n    sprite('Port_195_00', 3)\n", script);
    }
}
