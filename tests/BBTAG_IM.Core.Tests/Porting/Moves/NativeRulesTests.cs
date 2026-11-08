using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Porting.Moves;

namespace BBTAG_IM.Core.Tests.Porting.Moves;

public class NativeRulesTests
{
    private const string Commands = """
        cmd.Skill_236A <- {};
        cmd.Skill_236BorC <- {};
        cmd.Skill_236EX <- {};
        cmd.Skill_236BC <- {};
        cmd.Skill_236A_Add <- {};
        cmd.Skill_236B_Add <- {};
        cmd.Skill_214BorC <- {};
        cmd.Skill_214B_JAdd4BorC <- {};
        cmd.Atk_Std6C <- {};
        cmd.Atk_Std3C <- {};
        cmd.Atk_2C_2C <- {};
        cmd.Skill_LastArcBlast <- {};
        """;

    private const string Moves = """
        t.Mv_Skill_236A_Add <-
        {
            usepat="236_Add",
        }
        t.Mv_Skill_236B_Add <- t.Mv_Skill_236A_Add;

        t.Mv_Atk_AirC <-
        {
            function FrameUpdate_After()
            {
                Battle_Std.SetPattern_NotHoldButton( { ButtonMask=(1<<2), CheckFrameID=50, EndFrameID=60, SetPattern="AirC_End" } );
            }
        }

        t.Mv_Atk_StdC <-
        {
            function FrameUpdate_After()
            {
                switch( Battle_Std.GetUpdateFrameID() )
                {
                case 100:
                    BMvEff.CreateObject( { mvname="Mv_Obj_RollHouki", x=143, y=-253, flags=_Position_ToolShift } );
                    break;
                }
            }
        }
        """;

    private const string Table = """
        t.Skill_236A <-
        {
            num = def_CN_Skill_236A, name = "Cmd_Skill_236A", command = Def_CMD_236A,
            UpdateTable = { name = "Mv_Skill_236A", }
        }

        t.Skill_236BorC <-
        {
            num = def_CN_Skill_236B, name = "Cmd_Skill_236BorC", command = [Def_CMD_236B,Def_CMD_236C],
            UpdateTable = { name = "Mv_Skill_236B", }
        }

        t.Skill_236EX <-
        {
            num = def_CN_Skill_236EX, name = "Cmd_Skill_236EX", command = Def_CMD_236C, usegauge = 10000,
            UpdateTable = { name = "Mv_Skill_236EX", }
        }

        t.Skill_236BC <-
        {
            num = def_CN_Skill_236BC, name = "Cmd_Skill_236BC", command = ">+B+C",
            UpdateTable = { name = "Mv_Skill_236BC", }
        }

        t.Skill_236A_Add <-
        {
            num = def_CN_Skill_236A_Add, name = "Cmd_Skill_236A_Add", command = ["A","B","C"],
            CmdCheck = { SkillType="SpecialAdd", MvCheck={ Only=["Mv_Skill_236A"], SkillFail="Mv_Skill_236A_Add"} },
            UpdateTable = { name = "Mv_Skill_236A_Add", }
        }

        t.Atk_RapidRelayStdA <-
        {
            name = "Cmd_Atk_RapidRelayStdA",
            num = def_CN_Atk_RapidRelayStdA, command = ["A"],
            UpdateTable = { name = "Mv_Atk_RapidRelayPass_StdA", }
        }
        """;

    private const string Defines = """
        const Def_CMD_236A = "23@D,9@@N,6@6A@D,9@@N,8@@L,4@";
        const Def_CMD_236B = "23@D,9@@N,6@6B@D,9@@N,8@@L,4@";
        const Def_CMD_236C = "23@D,9@@N,6@6C@D,9@@N,8@@L,4@";
        const Def_Rule_EnableReverceBeat = 1;
        """;

    private const string Cpu = "Skill_Muteki = def_CN_Skill_236EX, // invincible move";

    private static readonly IReadOnlyDictionary<int, Ha6Pattern> Patterns = new[]
    {
        (1, ""), (2, ""), (3, ""), (4, ""), (5, ""), (6, ""), (7, ""), (8, ""), (9, ""), (26, "AirC_End"), (31, "RapidRelayAtk"), (111, "RollHouki"), (131, ""),
        (134, "2C_2C"), (175, "6C"), (195, "236A"), (196, "236B"), (197, "236EX"), (198, "236EX_Hit"), (194, "236BC"), (200, "236_Add"),
        (231, "214B"), (247, "214B_JAdd4B"), (326, "P_AtkB"), (401, "LastArc_Hit"), (407, "LastArc_End"),
    }.ToDictionary(entry => entry.Item1, entry => new Ha6Pattern(entry.Item1, "", [Ha6Frame.Empty], entry.Item2));

    private static IReadOnlyList<MoveDefinition> All() => MoveCatalog.Build(Commands, Moves, Patterns, NativeCommandTable.Parse(Table, Defines), Cpu);

    private static MoveDefinition Move(string state) => All().Single(move => move.State == state);

    [Theory]
    [InlineData("NmlAtk5C", "NmlAtk5A")]
    [InlineData("NmlAtk5C", "NmlAtk2B")]
    [InlineData("NmlAtk2C", "NmlAtk5B")]
    [InlineData("NmlAtk5B", "NmlAtk5A")]
    public void Build_ReverseBeat_LetsAHeavyNormalCancelIntoALighterOne(string from, string to)
    {
        // Act
        MoveDefinition move = Move(from);

        // Assert
        Assert.Contains(to, move.Cancels);
    }

    [Fact]
    public void Build_AirNormal_NeverCancelsIntoAGroundNormal()
    {
        // Act
        MoveDefinition move = Move("NmlAtkAIR5C");

        // Assert
        Assert.DoesNotContain("'NmlAtk4A'", move.Cancels);
    }

    [Fact]
    public void Build_Normal_NeverListsItself()
    {
        // Act
        MoveDefinition move = Move("NmlAtk5B");

        // Assert
        Assert.DoesNotContain("NmlAtk5B", move.Cancels);
    }

    [Fact]
    public void Build_WithoutReverseBeatInTheGame_OnlyChainsForward()
    {
        // Arrange
        NativeCommandTable table = NativeCommandTable.Parse(Table, "");

        // Act
        MoveDefinition move = MoveCatalog.Build(Commands, Moves, Patterns, table).Single(built => built.State == "NmlAtk5C");

        // Assert
        Assert.DoesNotContain("NmlAtk5A", move.Cancels);
    }

    [Theory]
    [InlineData("NmlAtk5A", "NmlAtk5A_2")]
    [InlineData("NmlAtk5A_2", "NmlAtk5A_3")]
    [InlineData("NmlAtk2A", "NmlAtk2A_2")]
    [InlineData("NmlAtk2A_2", "NmlAtk2A_3")]
    [InlineData("NmlAtkAIR5A", "NmlAtkAIR5A_2")]
    public void Build_LightNormal_RepeatsThreeTimes(string from, string to)
    {
        // Act
        MoveDefinition move = Move(from);

        // Assert
        Assert.Contains(to, move.Cancels);
    }

    [Theory]
    [InlineData("NmlAtk5A_2", "INPUT_4A")]
    [InlineData("NmlAtk2A_2", "INPUT_2A")]
    public void Build_Repeat_TakesTheInputOfTheGame(string state, string input)
    {
        // Act
        MoveDefinition move = Move(state);

        // Assert
        Assert.Equal(input, move.Input);
        Assert.Contains("FollowupOnly(1)", move.Requirements);
    }

    [Fact]
    public void Build_LastRepeat_HandsOverToTheRapidBeat()
    {
        // Act
        MoveDefinition move = Move("NmlAtk5A_3");

        // Assert
        Assert.Contains("'Rapid_4A'", move.Cancels);
        Assert.DoesNotContain("NmlAtk5A_4", move.Cancels);
    }

    [Fact]
    public void Build_BackA_PlaysTheStandingA()
    {
        // Act
        MoveDefinition move = Move("NmlAtk4A");

        // Assert
        Assert.Equal("NmlAtk5A", move.AliasOf);
        Assert.Contains("StateCall(NmlAtk5A)", move.Requirements);
    }

    [Theory]
    [InlineData("NmlAtk5A")]
    [InlineData("NmlAtk5B")]
    [InlineData("NmlAtk2C")]
    public void Build_RapidBeat_StartsFromTheLightMediumAndHeavyNormals(string state)
    {
        // Act
        MoveDefinition move = Move(state);

        // Assert
        Assert.Contains("NmlAtkRapidRelay", move.Cancels);
    }

    [Fact]
    public void Build_RapidBeat_EndsInTheFinisher()
    {
        // Act
        MoveDefinition move = Move("NmlAtkRapidRelay");

        // Assert
        Assert.Contains("NmlAtkRapidFinish", move.Cancels);
        Assert.Contains("'Finish_C'", move.Cancels);
    }

    [Fact]
    public void Build_SpecialNormals_DoNotStartARapidBeat()
    {
        // Act
        MoveDefinition move = Move("NmlAtk6C");

        // Assert
        Assert.DoesNotContain("NmlAtkRapidRelay", move.Cancels);
    }

    [Fact]
    public void Build_RapidAliases_RegisterBeforeTheNormalsSoNormalsWin()
    {
        // Act
        List<string> order = All().Select(move => move.State).ToList();

        // Assert
        Assert.True(order.IndexOf("Rapid_B") < order.IndexOf("NmlAtk5B"));
        Assert.True(order.IndexOf("NmlAtk5C") < order.IndexOf("Skill_236A"));
    }

    [Fact]
    public void Build_SecondInputOfASpecial_BecomesAnAliasRegisteredFirst()
    {
        // Act
        List<MoveDefinition> moves = All().ToList();
        MoveDefinition alias = moves.Single(move => move.State == "Skill_236BorC_Alt0");

        // Assert
        Assert.Equal("Skill_236BorC", alias.AliasOf);
        Assert.Contains("Move_Input_(INPUT_PRESS_C)", alias.Requirements);
        Assert.Contains("StateCall(Skill_236BorC)", alias.Requirements);
        Assert.True(moves.IndexOf(alias) < moves.FindIndex(move => move.State == "Skill_236EX"));
    }

    [Fact]
    public void Build_ExSpecial_SpendsMeterAndFlashes()
    {
        // Act
        MoveDefinition move = Move("Skill_236EX");

        // Assert
        Assert.True(move.MeterCost > 0);
        Assert.True(move.Flashes);
    }

    [Fact]
    public void Build_ExSpecialWithAHitPattern_BranchesOnHit()
    {
        // Act
        MoveDefinition move = Move("Skill_236EX");

        // Assert
        Assert.Equal([198], move.HitPatterns);
    }

    [Fact]
    public void Build_MoonSkill_UsesItsDirectionAndBothButtons()
    {
        // Act
        MoveDefinition move = Move("Skill_236BC");

        // Assert
        Assert.Contains("Move_Input_(INPUT_HOLD_FORWARD)", move.Requirements);
        Assert.Contains("Move_Input_(INPUT_PRESS_BC)", move.Requirements);
        Assert.DoesNotContain("Move_Input_(INPUT_236)", move.Requirements);
    }

    [Theory]
    [InlineData("Skill_236A", "Skill_236A_Add")]
    [InlineData("Skill_236BorC", "Skill_236B_Add")]
    [InlineData("Skill_214BorC", "Skill_214B_JAdd4BorC")]
    public void Build_Followup_ComesOutOfTheMoveThatTakesTwoButtonsToo(string parent, string followup)
    {
        // Act
        MoveDefinition move = Move(parent);

        // Assert
        Assert.Contains(followup, move.Followups);
    }

    [Fact]
    public void Build_FollowupSharingATemplate_FindsItsPattern()
    {
        // Act
        MoveDefinition move = Move("Skill_236B_Add");

        // Assert
        Assert.Equal([200], move.Patterns);
    }

    [Fact]
    public void Build_HeldAirNormal_KnowsItsEndPattern()
    {
        // Act
        MoveDefinition move = Move("NmlAtkAIR5C");

        // Assert
        Assert.Equal("C", move.HoldButton);
        Assert.Equal([9, 26], move.Patterns);
    }

    [Fact]
    public void Build_NormalThatCreatesAnObject_SpawnsItOnItsFrameId()
    {
        // Act
        MoveDefinition move = Move("NmlAtk5C");

        // Assert
        Assert.Equal([new MoveObject(100, "RollHouki", 143, -253)], move.Objects);
    }

    [Fact]
    public void Build_Assist_UsesThePartnerAnimationAndReplacesTheCommonState()
    {
        // Act
        MoveDefinition move = Move("CmnActChangePartnerAssistAtk_B");

        // Assert
        Assert.True(move.ReplacesCommonState);
        Assert.False(move.IsRegistered);
        Assert.Equal([326], move.Patterns);
    }

    [Fact]
    public void Build_Reversal_IsTheInvincibleMoveTheCpuUses()
    {
        // Act
        MoveDefinition move = Move("CmnActInvincibleAttack");

        // Assert
        Assert.Equal(MoveKind.Reversal, move.Kind);
        Assert.Equal([197], move.Patterns);
    }

    [Fact]
    public void Build_LastArc_BecomesTheAstralHeat()
    {
        // Act
        MoveDefinition move = Move("AstralHeat");

        // Assert
        Assert.Equal("INPUT_ASTRAL", move.Input);
        Assert.Equal([401, 407], move.Patterns);
    }
}
