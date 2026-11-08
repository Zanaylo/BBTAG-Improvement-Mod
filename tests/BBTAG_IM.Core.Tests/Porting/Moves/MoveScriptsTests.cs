using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Porting.Moves;

namespace BBTAG_IM.Core.Tests.Porting.Moves;

public class MoveScriptsTests
{
    private const string Moves = """
        local maketmpl_Item = function( param={} )
        {
            switch( param.type )
            {
            case "A":
                mvParam.itemParam = { x=50, y=-83, mvname="Mv_AtkLandObj_DrillTree", flags=_Position_ToolShift };
                break;
            }
        }

        t.Mv_Skill_0202A <- maketmpl_Item( { type="A" } );
        """;

    private static readonly IReadOnlyDictionary<int, Ha6Pattern> Patterns = new Dictionary<int, Ha6Pattern>
    {
        [1] = new(1, "", [new Ha6Frame()]),
        [270] = new(270, "", [new Ha6Frame()], "0202A"),
        [280] = new(280, "", [new Ha6Frame()], "DrillTree"),
    };

    [Fact]
    public void Build_Commands_RegisterEveryMove()
    {
        // Act
        MoveScripts scripts = MoveScripts.Build("cmd.Skill_0202A <- {};", Moves, "", Patterns, "Port_");

        // Assert
        Assert.Contains("def NmlAtk5A(INPUT_5A):", scripts.Registrations);
        Assert.Contains("def Skill_0202A(INPUT_SPECIALMOVE):", scripts.Registrations);
    }

    [Fact]
    public void Build_Commands_WriteEveryState()
    {
        // Act
        MoveScripts scripts = MoveScripts.Build("cmd.Skill_0202A <- {};", Moves, "", Patterns, "Port_");

        // Assert
        Assert.Contains("def NmlAtk5A():", scripts.States);
        Assert.Contains("def Skill_0202A():", scripts.States);
    }

    [Fact]
    public void Build_MoveWithObject_WritesTheObjectOnce()
    {
        // Act
        MoveScripts scripts = MoveScripts.Build("cmd.Skill_0202A <- {};\ncmd.Skill_0202A <- {};", Moves, "", Patterns, "Port_");

        // Assert
        Assert.Single(scripts.Objects.Split("def Obj_DrillTree():")[1..]);
    }
}
