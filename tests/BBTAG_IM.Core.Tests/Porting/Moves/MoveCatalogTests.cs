using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Porting.Moves;

namespace BBTAG_IM.Core.Tests.Porting.Moves;

public class MoveCatalogTests
{
    private const string Commands = """
        cmd.Skill_236A <- {};
        cmd.Skill_236BorC <- {};
        cmd.Skill_236EX <- {};
        cmd.Skill_J214A <- {};
        cmd.Skill_0202A <- {};
        cmd.Skill_41236SP <- {};
        cmd.Skill_214A <- {};
        cmd.Skill_214A_JAdd <- {};
        cmd.Skill_214A_JAdd4BorC <- {};
        cmd.Skill_M_ChangeTouch <- {};
        cmd.Atk_Std6C <- {};
        cmd.Atk_Std3C <- {};
        cmd.Atk_2C_2C <- {};
        """;

    private const string Moves = """
        local maketmpl_SkillDropBomb = function( param={} )
        {
            switch( param.type )
            {
            case "A":
                break;
            case "A_Add":
                mvParam.ball = { x=0, y=-250, mv="Mv_FireBall_214A", pat="DropBombGro" };
                break;
            case "A_Add4B":
                ret_tmpl.usepat <- "214_JAdd4B";
                mvParam.ball = { x=0, y=-250, mv="Mv_FireBall_214A", pat="DropBomb4" };
                break;
            }
        }

        t.Mv_Skill_214A <- maketmpl_SkillDropBomb( { type="A" } );
        t.Mv_Skill_214A_JAdd <- maketmpl_SkillDropBomb( { type="A_Add" } );
        t.Mv_Skill_214A_JAdd4B <- maketmpl_SkillDropBomb( { type="A_Add4B" } );
        """;

    private static readonly IReadOnlyDictionary<int, Ha6Pattern> Patterns = new[]
    {
        (1, ""), (2, ""), (3, ""), (4, ""), (5, ""), (6, ""), (7, ""), (8, ""), (9, ""), (131, ""),
        (134, "2C_2C"), (175, "6C"), (178, "6C_End"), (195, "236A"), (192, "236A_End"), (196, "236B"), (197, "236EX"),
        (250, "J214A"), (270, "0202A"), (350, "41236SP"), (230, "214A"), (235, "214A_JAdd"), (247, "214_JAdd4B"), (245, "DropBomb4"),
    }.ToDictionary(entry => entry.Item1, entry => new Ha6Pattern(entry.Item1, "", [Ha6Frame.Empty], entry.Item2));

    private static MoveDefinition Move(string state) => MoveCatalog.Build(Commands, Moves, Patterns).Single(move => move.State == state);

    [Theory]
    [InlineData("NmlAtk5A", "INPUT_5A", 1)]
    [InlineData("NmlAtk5B", "INPUT_5B", 2)]
    [InlineData("NmlAtk5C", "INPUT_5C", 3)]
    [InlineData("NmlAtk2A", "INPUT_2A", 4)]
    [InlineData("NmlAtk2B", "INPUT_2B", 5)]
    [InlineData("NmlAtk2C", "INPUT_2C", 6)]
    [InlineData("NmlAtkAIR5A", "INPUT_J5A", 7)]
    [InlineData("NmlAtkAIR5B", "INPUT_J5B", 8)]
    [InlineData("NmlAtkAIR5C", "INPUT_J5C", 9)]
    [InlineData("NmlAtk3C", "INPUT_3C", 131)]
    public void Build_Normals_UseTheStandardPatterns(string state, string input, int pattern)
    {
        // Act
        MoveDefinition move = Move(state);

        // Assert
        Assert.Equal(input, move.Input);
        Assert.Equal([pattern], move.Patterns);
    }

    [Fact]
    public void Build_6C_RunsItsEndPatternToo()
    {
        // Act
        MoveDefinition move = Move("NmlAtk6C");

        // Assert
        Assert.Equal("INPUT_6C", move.Input);
        Assert.Equal([175, 178], move.Patterns);
    }

    [Fact]
    public void Build_ChainedNormal_ListsTheHeavierNormals()
    {
        // Act
        MoveDefinition move = Move("NmlAtk5A");

        // Assert
        Assert.Contains("NmlAtk5B", move.Cancels);
        Assert.Contains("NmlAtk2C", move.Cancels);
    }

    [Fact]
    public void Build_Special_TakesMotionAndButton()
    {
        // Act
        MoveDefinition move = Move("Skill_236BorC");

        // Assert
        Assert.Equal("INPUT_SPECIALMOVE", move.Input);
        Assert.Equal(MoveKind.StandingSpecial, move.Kind);
        Assert.Equal(["Move_AirGround_(MOVE_GROUND)", "Move_Input_(INPUT_236)", "Move_Input_(INPUT_PRESS_B)"], move.Requirements);
        Assert.Equal([196], move.Patterns);
    }

    [Fact]
    public void Build_HeldSpecial_KnowsItsButton()
    {
        // Act
        MoveDefinition move = Move("Skill_236A");

        // Assert
        Assert.Equal("A", move.HoldButton);
        Assert.Equal([195, 192], move.Patterns);
    }

    [Fact]
    public void Build_ExSpecial_UsesTheCButtonAndTheExCost()
    {
        // Act
        MoveDefinition move = Move("Skill_236EX");

        // Assert
        Assert.Equal(["Move_AirGround_(MOVE_GROUND)", "Move_AirGround_(MOVE_EX)", "Move_Input_(INPUT_236)", "Move_Input_(INPUT_PRESS_C)"], move.Requirements);
    }

    [Fact]
    public void Build_AirSpecial_IsOnlyAvailableInTheAir()
    {
        // Act
        MoveDefinition move = Move("Skill_J214A");

        // Assert
        Assert.Equal(MoveKind.AirSpecial, move.Kind);
        Assert.Equal(["Move_AirGround_(MOVE_AIR)", "Move_Input_(INPUT_214)", "Move_Input_(INPUT_PRESS_A)"], move.Requirements);
    }

    [Fact]
    public void Build_DoubleDownMotion_BecomesTheNative22()
    {
        // Act
        MoveDefinition move = Move("Skill_0202A");

        // Assert
        Assert.Contains("Move_Input_(INPUT_22)", move.Requirements);
        Assert.Equal([270], move.Patterns);
    }

    [Fact]
    public void Build_ArcDrive_IsASuper()
    {
        // Act
        MoveDefinition move = Move("Skill_41236SP");

        // Assert
        Assert.Equal("INPUT_DISTORTION", move.Input);
        Assert.Equal(MoveKind.Super, move.Kind);
        Assert.Equal(["Move_AirGround_(MOVE_GROUND)", "Move_AirGround_(MOVE_DISTORTION)", "Move_Input_(INPUT_41236)", "Move_Input_(INPUT_PRESS_BC)"], move.Requirements);
    }

    [Fact]
    public void Build_Followup_OnlyComesOutOfItsParent()
    {
        // Act
        MoveDefinition parent = Move("Skill_214A");
        MoveDefinition followup = Move("Skill_214A_JAdd");

        // Assert
        Assert.Contains("Skill_214A_JAdd", parent.Followups);
        Assert.Equal(["Move_Input_(INPUT_0xed)", "FollowupOnly(1)"], followup.Requirements);
    }

    [Fact]
    public void Build_Followup_ReadsObjectAndPatternFromItsTemplate()
    {
        // Act
        MoveDefinition followup = Move("Skill_214A_JAdd4BorC");

        // Assert
        Assert.Equal([247], followup.Patterns);
        Assert.Equal([new MoveObject(100, "DropBomb4", 0, -250)], followup.Objects);
    }

    [Fact]
    public void Build_ObjectWithoutPattern_IsLeftOut()
    {
        // Act
        MoveDefinition followup = Move("Skill_214A_JAdd");

        // Assert
        Assert.Empty(followup.Objects);
    }

    [Fact]
    public void Build_CommandWithoutPattern_IsLeftOut()
    {
        // Act
        IReadOnlyList<MoveDefinition> moves = MoveCatalog.Build("cmd.Skill_623A <- {};", "", Patterns);

        // Assert
        Assert.DoesNotContain(moves, move => move.State == "Skill_623A");
    }

    [Fact]
    public void Build_PartnerCommand_IsLeftOut()
    {
        // Act
        IReadOnlyList<MoveDefinition> moves = MoveCatalog.Build(Commands, Moves, Patterns);

        // Assert
        Assert.DoesNotContain(moves, move => move.State.Contains("Skill_M_", StringComparison.Ordinal));
    }
}
