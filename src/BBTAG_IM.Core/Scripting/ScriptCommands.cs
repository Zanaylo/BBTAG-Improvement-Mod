namespace BBTAG_IM.Core.Scripting;

public static class ScriptCommands
{
    public const int StartState = 0;
    public const int EndState = 1;
    public const int If = 4;
    public const int EndIf = 5;
    public const int StartSubroutine = 8;
    public const int EndSubroutine = 9;
    public const int Upon = 15;
    public const int EndUpon = 16;
    public const int EndObjectFunction = 35;
    public const int ObjectFunction = 36;
    public const int Operation = 40;
    public const int StoreValue = 41;
    public const int OperationInto = 47;
    public const int ModifyValue = 49;
    public const int IfNot = 54;
    public const int EndIfNot = 55;
    public const int Else = 56;
    public const int EndElse = 57;
    public const int StateRegister = 14001;
    public const int EndStateRegister = 14002;

    public static readonly IReadOnlySet<int> WritesSlotZero = new HashSet<int>
    {
        39, 40, 42, 43, 44, 45, 46, 59, 60, 61, 63, 66, 69, 70, 1116, 2065, 23036, 23037, 23045, 23145, 23146,
        23148, 23156, 23166, 23177, 30042,
    };

    private static readonly Dictionary<int, int> Closers = new()
    {
        [StartState] = EndState,
        [StartSubroutine] = EndSubroutine,
        [Upon] = EndUpon,
        [ObjectFunction] = EndObjectFunction,
        [If] = EndIf,
        [IfNot] = EndIfNot,
        [Else] = EndElse,
        [StateRegister] = EndStateRegister,
    };

    private static readonly HashSet<int> AllClosers = [.. Closers.Values];

    public static int? CloserOf(int opener) => Closers.TryGetValue(opener, out var closer) ? closer : null;

    public static bool IsCloser(int id) => AllClosers.Contains(id);

    public static bool StartsFunction(int id) => id is StartState or StartSubroutine;
}
