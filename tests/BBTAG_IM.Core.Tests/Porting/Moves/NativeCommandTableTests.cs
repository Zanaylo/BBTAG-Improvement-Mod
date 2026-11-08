using BBTAG_IM.Core.Porting.Moves;

namespace BBTAG_IM.Core.Tests.Porting.Moves;

public class NativeCommandTableTests
{
    private const string Defines = """
        const Def_CMD_236A = "23@D,9@@N,6@6A@D,9@@N,8@@L,4@";
        // const Def_CMD_236A = "236A";
        const Def_CMD_236B = "23@D,9@@N,6@6B@D,9@@N,8@@L,4@";
        const Def_CMD_236C = "23@D,9@@N,6@6C@D,9@@N,8@@L,4@";
        const Def_Rule_EnableReverceBeat = 1; // reverse beat
        """;

    private const string Table = """
        t.Skill_236BorC <-
        { //-------------------------------
            num = def_CN_Skill_236B, name = "Cmd_Skill_236BorC", command = [Def_CMD_236B,Def_CMD_236C],
            CmdCheck = { MvCheck={ SkillFail="Mv_Skill_236B"} },
            UpdateTable = { name = "Mv_Skill_236B", }
        }

        t.Skill_236EX <-
        { //-------------------------------
            num = def_CN_Skill_236EX, name = "Cmd_Skill_236EX", command = Def_CMD_236C, usegauge = 10000,
            UpdateTable = { name = "Mv_Skill_236EX", }
        }

        t.Skill_214A_JAdd <-
        { //-------------------------------
            num = def_CN_Skill_214A_JAdd, name = "Cmd_Skill_214A_JAdd", command = ["A","B","C"], posstatus = _PosState_Air,
            CmdCheck = { SkillType="SpecialAdd", MvCheck={ Only=["Mv_Skill_214A"], SkillFail="Mv_Skill_214A_JAdd"} },
            UpdateTable = { name = "Mv_Skill_214A_JAdd", }
        }

        t.Skill_236BC <-
        { //-------------------------------
            num = def_CN_Skill_236BC, name = "Cmd_Skill_236BC", command = ">+B+C",
            UpdateTable = { name = "Mv_Skill_236BC", }
        }
        """;

    private static NativeCommandTable Parsed() => NativeCommandTable.Parse(Table, Defines);

    [Fact]
    public void Parse_CommandInConstants_ReadsTheInputWithoutItsTimings()
    {
        // Act
        NativeCommand? command = Parsed().Find("Skill_236BorC");

        // Assert
        Assert.Equal(["236B", "236C"], command?.Inputs);
    }

    [Fact]
    public void Parse_GaugeCost_IsKept()
    {
        // Act
        NativeCommand? command = Parsed().Find("Skill_236EX");

        // Assert
        Assert.Equal(10000, command?.Cost);
    }

    [Fact]
    public void Parse_Followup_KnowsItsParentAndThatItIsInTheAir()
    {
        // Act
        NativeCommand? command = Parsed().Find("Skill_214A_JAdd");

        // Assert
        Assert.NotNull(command);
        Assert.True(command.InAir);
        Assert.Equal(["Mv_Skill_214A"], command.OnlyFrom);
        Assert.Equal(["A", "B", "C"], command.Inputs);
    }

    [Fact]
    public void Parse_MoveTable_IsKept()
    {
        // Act
        NativeCommand? command = Parsed().Find("Skill_236BC");

        // Assert
        Assert.Equal("Mv_Skill_236BC", command?.MoveName);
    }

    [Fact]
    public void Find_UnknownCommand_ReturnsNull()
    {
        // Act
        NativeCommand? command = Parsed().Find("Skill_999A");

        // Assert
        Assert.Null(command);
    }

    [Theory]
    [InlineData("const Def_Rule_EnableReverceBeat = 1;", true)]
    [InlineData("const Def_Rule_EnableReverceBeat = 0;", false)]
    [InlineData("", false)]
    public void HasReverseBeat_ReadsTheRuleOfTheGame(string defines, bool expected)
    {
        // Act
        NativeCommandTable table = NativeCommandTable.Parse(Table, defines);

        // Assert
        Assert.Equal(expected, table.HasReverseBeat);
    }
}
