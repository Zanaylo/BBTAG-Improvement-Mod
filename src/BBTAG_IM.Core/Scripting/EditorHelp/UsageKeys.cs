namespace BBTAG_IM.Core.Scripting.EditorHelp;

public static class UsageKeys
{
    public static UsageKey? Resolve(ScriptDatabase database, string name)
    {
        if (database.Find(name) is { } command)
            return new UsageKey(UsageKind.Command, command.Id);

        if (database.Inputs.Normal.ValueOf(name) is { } normal && database.Inputs.Motions.ValueOf(name) is null)
            return new UsageKey(UsageKind.NormalInput, normal);

        if (database.Inputs.ValueOf(name) is { } input)
            return new UsageKey(UsageKind.MoveInput, input);

        if (database.MoveConditions.ValueOf(name) is { } condition)
            return new UsageKey(UsageKind.MoveCondition, condition);

        if (database.UponNumber(name) is { } upon)
            return new UsageKey(UsageKind.Upon, upon);

        return database.HitAnimations.ValueOf(name) is { } animation ? new UsageKey(UsageKind.HitAnimation, animation) : null;
    }
}
