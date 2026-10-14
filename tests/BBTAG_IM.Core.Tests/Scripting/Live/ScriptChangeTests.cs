using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class ScriptChangeTests
{
    [Fact]
    public void Between_OnlyAValueChanged_ReturnsAnInPlaceWriteOfThatValue()
    {
        // Arrange
        ScriptBinary current = Read(new ScriptBytes().State("A").Command(ScriptTestDatabase.Damage, 12).Command(1).Build());
        ScriptBinary next = Read(new ScriptBytes().State("A").Command(ScriptTestDatabase.Damage, 15).Command(1).Build());
        int valueOffset = ScriptBinary.DataStartFor(1) + 36 + 4;

        // Act
        ScriptChange change = ScriptChange.Between(current, next);

        // Assert
        ScriptWrite write = Assert.Single(Assert.IsType<InPlaceChange>(change).Writes);
        Assert.Equal(valueOffset, write.Offset);
        Assert.Equal([15], write.Bytes);
    }

    [Fact]
    public void Between_CommandAdded_ReturnsAMoveWithTheNewStartOfLaterFunctions()
    {
        // Arrange
        ScriptBinary current = Read(new ScriptBytes().State("A").Command(1).Subroutine("S").Command(9).State("B").Command(1).Build());
        ScriptBinary next = Read(new ScriptBytes().State("A").Command(ScriptTestDatabase.Damage, 1).Command(1).Subroutine("S").Command(9).State("B").Command(1).Build());

        // Act
        ScriptChange change = ScriptChange.Between(current, next);

        // Assert
        MovedChange moved = Assert.IsType<MovedChange>(change);
        Assert.Equal(new Dictionary<int, int> { [0] = 0, [40] = 48, [80] = 88 }, moved.Offsets);
        Assert.Empty(moved.AddedFunctions);
    }

    [Fact]
    public void Between_NewState_ListsItAsAdded()
    {
        // Arrange
        ScriptBinary current = Read(new ScriptBytes().State("A").Command(1).Build());
        ScriptBinary next = Read(new ScriptBytes().State("A").Command(1).State("B").Command(1).Build());

        // Act
        ScriptChange change = ScriptChange.Between(current, next);

        // Assert
        Assert.Equal(["B"], Assert.IsType<MovedChange>(change).AddedFunctions);
    }

    [Fact]
    public void Between_SameBytes_ReturnsAnInPlaceChangeWithNoWrites()
    {
        // Arrange
        ScriptBinary current = Read(new ScriptBytes().State("A").Command(1).Build());

        // Act
        ScriptChange change = ScriptChange.Between(current, current);

        // Assert
        Assert.Empty(Assert.IsType<InPlaceChange>(change).Writes);
    }

    private static ScriptBinary Read(byte[] bytes) => ScriptBinary.Read(bytes, ScriptTestDatabase.Create());
}
