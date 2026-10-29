namespace BBTAG_IM.Core.Scripting;

public enum ValueKind
{
    Number,
    Input,
    Upon,
    HitAnimation,
    MoveCondition,
}

public static class ValueKinds
{
    private static readonly HashSet<int> InputCommands = [43, 14001, 14012];
    private static readonly HashSet<int> UponFirstCommands = [15, 17, 28, 29, 30, 31];
    private static readonly HashSet<int> UponSecondCommands = [21007, 21012, 21013];
    private static readonly HashSet<int> HitAnimationCommands = [9322, 9324, 9334, 9336];
    private static readonly HashSet<int> MoveConditionCommands = [14003];

    private static readonly HashSet<(int Command, int Index)> StateReferences =
    [
        (10, 0), (14001, 0), (21, 0), (24, 0), (28, 1), (11047, 0), (11069, 0), (12059, 1), (14013, 0), (14019, 0), (14024, 0),
        (14027, 0), (14048, 0), (14049, 0), (14049, 1), (14068, 0), (14069, 0), (14070, 0), (14071, 0), (14072, 0),
        (14073, 0), (14074, 0), (14080, 0), (14084, 0), (14085, 0), (14086, 0), (15024, 0), (15024, 1), (16001, 0),
        (17011, 0), (23145, 0), (23146, 1), (23148, 0),
    ];

    public static ValueKind Of(int command, int index)
    {
        if (InputCommands.Contains(command))
            return ValueKind.Input;

        if (UponFirstCommands.Contains(command) && index == 0)
            return ValueKind.Upon;

        if (UponSecondCommands.Contains(command) && index == 1)
            return ValueKind.Upon;

        if (HitAnimationCommands.Contains(command))
            return ValueKind.HitAnimation;

        if (MoveConditionCommands.Contains(command))
            return ValueKind.MoveCondition;

        return ValueKind.Number;
    }

    public static bool UsesInputs(int command) => InputCommands.Contains(command);

    public static bool IsStateReference(int command, int index) => StateReferences.Contains((command, index));
}
