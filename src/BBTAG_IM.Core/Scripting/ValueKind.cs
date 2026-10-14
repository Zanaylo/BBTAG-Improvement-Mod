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
    private static readonly HashSet<int> UponFirstCommands = [17, 28, 29, 30, 31];
    private static readonly HashSet<int> UponSecondCommands = [21007, 21012, 21013];
    private static readonly HashSet<int> HitAnimationCommands = [9322, 9324, 9334, 9336];
    private static readonly HashSet<int> MoveConditionCommands = [14003];

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
}
