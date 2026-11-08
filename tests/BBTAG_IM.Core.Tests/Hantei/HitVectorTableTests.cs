using BBTAG_IM.Core.Hantei;

namespace BBTAG_IM.Core.Tests.Hantei;

public class HitVectorTableTests
{
    private const string Table = """
        [ 112,  1400, -3700,    0,  250 ], //  note
        [   1,  1500,     0,  -80,    0 ], //  note

        vectmpl.add({ number = 1, name = "stand_B1",
            tmpl_param = { type="yarare_stand", power=def_VL_AtkLv_B1, vector_num=1, hit_pattern = def_VLP_Atama2 },
        })
        vectmpl.add({ number = 112, name = "uki_C1",
            tmpl_param = { type="uki", power=def_VL_AtkLv_C1, },
            tbl =
            [
                {
                    vector_num = 112,
                },
                {
                    vector_num = 193,
                },
            ]
        })
        """;

    [Fact]
    public void Find_StandingHit_DoesNotLaunch()
    {
        // Arrange
        HitVectorTable table = HitVectorTable.Parse(Table);

        // Act
        HitVector? vector = table.Find(1);

        // Assert
        Assert.Equal(new HitVector(1, false, 3, 1500, 0), vector);
    }

    [Fact]
    public void Find_LaunchingHit_UsesTheFirstVectorOfItsTable()
    {
        // Arrange
        HitVectorTable table = HitVectorTable.Parse(Table);

        // Act
        HitVector? vector = table.Find(112);

        // Assert
        Assert.Equal(new HitVector(112, true, 4, 1400, -3700), vector);
    }

    [Fact]
    public void Find_UnknownNumber_ReturnsNothing()
    {
        // Arrange
        HitVectorTable table = HitVectorTable.Parse(Table);

        // Act
        HitVector? vector = table.Find(7);

        // Assert
        Assert.Null(vector);
    }
}
