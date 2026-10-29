using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.EditorHelp;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.EditorHelp;

public class UsageKeysTests
{
    [Theory]
    [InlineData("Damage", UsageKind.Command, ScriptTestDatabase.Damage)]
    [InlineData("Unknown9000", UsageKind.Command, ScriptTestDatabase.Damage)]
    [InlineData("INPUT_5A", UsageKind.NormalInput, 0x07)]
    [InlineData("INPUT_236", UsageKind.MoveInput, 172)]
    [InlineData("INPUT_HOLD_6", UsageKind.MoveInput, 0x78)]
    [InlineData("MOVE_EX", UsageKind.MoveCondition, 0x3086)]
    [InlineData("upon_Hit", UsageKind.Upon, 10)]
    [InlineData("AIR_VERTICAL", UsageKind.HitAnimation, 10)]
    public void Resolve_Name_GivesItsKey(string name, UsageKind kind, int value)
    {
        // Arrange
        ScriptDatabase database = ScriptTestDatabase.Create().WithTagNames();

        // Act
        UsageKey? key = UsageKeys.Resolve(database, name);

        // Assert
        Assert.Equal(new UsageKey(kind, value), key);
    }

    [Fact]
    public void Resolve_UnknownName_ReturnsNull()
    {
        // Arrange
        ScriptDatabase database = ScriptTestDatabase.Create().WithTagNames();

        // Act
        UsageKey? key = UsageKeys.Resolve(database, "NotAThing");

        // Assert
        Assert.Null(key);
    }
}
