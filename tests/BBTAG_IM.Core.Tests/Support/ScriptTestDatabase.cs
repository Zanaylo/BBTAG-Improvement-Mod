using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Tests.Support;

internal static class ScriptTestDatabase
{
    public const int Damage = 9000;
    public const int ClearUpon = 17;
    public const int CheckInput = 43;
    public const int HitAnimation = 9322;
    public const int SharedGatling = 14027;

    public static ScriptDatabase Create() =>
        new(
            [
                Command(0, "startState", "32s"),
                Command(1, "endState", ""),
                Command(2, "sprite", "32si"),
                Command(4, "If", "ii", slots: [0]),
                Command(5, "endIf", ""),
                Command(8, "startSubroutine", "32s"),
                Command(9, "endSubroutine", ""),
                Command(10, "callSubroutine", "32s"),
                Command(15, "upon", "i"),
                Command(16, "endUpon", ""),
                Command(ClearUpon, "clearUponHandler", "i"),
                Command(35, "endObjectFunction", ""),
                Command(36, "ApplyFunctionsToObjects", "i"),
                Command(40, "op", "iiiii", slots: [1, 3]),
                Command(41, "StoreValue", "iiii", slots: [0, 2]),
                Command(CheckInput, "CheckInput", "i", hex: HexArguments.All),
                Command(47, "PrivateFunction", "7i", slots: [1, 3, 5]),
                Command(49, "ModifyVar_", "5i", slots: [1, 3]),
                Command(54, "IfNot", "ii", slots: [0]),
                Command(55, "endIfNot", ""),
                Command(56, "Else", ""),
                Command(57, "endElse", ""),
                Command(Damage, "Damage", "i"),
                Command(HitAnimation, "GroundedHitstunAnimation", "i"),
                Command(SharedGatling, "SharedGatling", "32s"),
                Command(14001, "Move_Register", "32si", hex: HexArguments.All),
                Command(14002, "Move_EndRegister", ""),
            ],
            new NameTable([new(51, "IsInOverdrive")]),
            new NameTable([new(0, "On_Initialize"), new(10, "Hit")]),
            new NameTable([new(10, "AIR_VERTICAL")]),
            NameTable.Empty,
            new InputNames(
                new NameTable([new(172, "INPUT_236")]),
                new ButtonByteInputs(
                    new NameTable([new(2, "INPUT_SPECIALMOVE")]),
                    new NameTable([new(1, "A"), new(2, "B")]),
                    new NameTable([new(4, "INPUT_5"), new(1, "INPUT_2")]))));

    private static CommandDefinition Command(int id, string name, string format, int[]? slots = null, HexArguments? hex = null) =>
        new(id, name, CommandFormat.Parse(format), new HashSet<int>(slots ?? []), hex ?? HexArguments.None);
}
