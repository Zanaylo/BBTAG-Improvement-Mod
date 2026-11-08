using BBTAG_IM.Core.Porting.Moves;

namespace BBTAG_IM.Core.Tests.Porting.Moves;

public class CharacterStatsTests
{
    private const string MoveTable = """
        {
            local type = "walk";
            chrparam.Param[type] <- array(CHRMAX,    [512, 1250, -512,  -900] );
            chrparam.Param[type][Def_ChrNo_Chr004] = [512, 1280, -512, -1024];
        }
        {
            local type = "jump";
            chrparam.Param[type] <- array(CHRMAX, [-4300, 256, 1280, 0, -1024] );
        }
        {
            local type = "high";
            chrparam.Param[type] <- array(CHRMAX, [-5120, 256, 2560, 0, -1280] );
        }
        {
            local type = "dash";
            chrparam.Param[type] <- array(CHRMAX, { [0]={ x=128, addx=32 } } );
            chrparam.Param[type][Def_ChrNo_Chr004] = { [0]={ x=128, addx=32 }, [3]={ x= 700, addx=230 }, [5]={ x=1280, addx=256, max=5120, effect=1 } };
        }
        local walk_vectorar = chrparam.Get( { type="walk", chrnum=_ChrNo } );
        local jump_vectorar = chrparam.Get( { type="jump", chrnum=_ChrNo } );
        local highjump_vectorar = chrparam.Get( { type="high", chrnum=_ChrNo } );
        local dash_f_vector_tbl = chrparam.Get( { type="dash", chrnum=_ChrNo } );
        """;

    private const string Status = """
        Battle_Status <-
        {
            HpDivValue = [ 11000 ],
        }
        Battle_Status <-
        {
            HpDivValue = [ 11700 ],
        }
        """;

    private const string StandardStatus = "HpDivValue = [ 11000 ],";

    private static IReadOnlyList<string> Lines() => CharacterStats.Parse(MoveTable, Status, StandardStatus, "chr004").Lines;

    [Theory]
    [InlineData("WalkFSpeed(10000)")]
    [InlineData("WalkBSpeed(8000)")]
    public void Parse_Walk_TakesTheCharacterRowInNativeUnits(string expected)
    {
        // Act
        IReadOnlyList<string> lines = Lines();

        // Assert
        Assert.Contains(expected, lines);
    }

    [Theory]
    [InlineData("JumpYVelocity(33594)")]
    [InlineData("Gravity(2000)")]
    [InlineData("InitFJumpXSpeed(10000)")]
    [InlineData("InitBJumpXSpeed(8000)")]
    [InlineData("SuperJumpYVelocity(40000)")]
    [InlineData("InitFHighJumpXSpeed(20000)")]
    [InlineData("InitBHighJumpXSpeed(10000)")]
    public void Parse_Jump_FallsBackToTheSharedRow(string expected)
    {
        // Act
        IReadOnlyList<string> lines = Lines();

        // Assert
        Assert.Contains(expected, lines);
    }

    [Theory]
    [InlineData("DashFInitialVelocity(10000)")]
    [InlineData("DashFAccel(2000)")]
    [InlineData("DashFMaxVelocity(40000)")]
    public void Parse_Dash_UsesItsLastStep(string expected)
    {
        // Act
        IReadOnlyList<string> lines = Lines();

        // Assert
        Assert.Contains(expected, lines);
    }

    [Fact]
    public void Parse_Health_KeepsThePlaceInTheRoster()
    {
        // Act
        IReadOnlyList<string> lines = Lines();

        // Assert
        Assert.Contains("Health(17000)", lines);
    }

    [Fact]
    public void DamageScale_IsTheRatioBetweenTheTwoHealthScales()
    {
        // Act
        double scale = CharacterStats.Parse(MoveTable, Status, StandardStatus, "chr004").DamageScale;

        // Assert
        Assert.Equal(16000.0 / 11000.0, scale, 6);
    }

    [Fact]
    public void Parse_TablesWithoutTheParameters_WritesNoLines()
    {
        // Act
        CharacterStats stats = CharacterStats.Parse("", "", "", "chr004");

        // Assert
        Assert.Empty(stats.Lines);
        Assert.Equal(16000.0 / 11000.0, stats.DamageScale, 6);
    }
}
