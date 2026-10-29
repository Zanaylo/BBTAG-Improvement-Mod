namespace BBTAG_IM.Core.Game;

public enum ErrorBoxAction
{
    ShowBox,
    Continue,
    BackToCharacterSelect,
    ScriptRolledBack,
}

public static class ErrorBoxPolicy
{
    public const string FatalTitle = "Error";
    public const string WarningTitle = "Continue";

    public static bool IsGameError(string caption) => caption is FatalTitle or WarningTitle;

    public static ErrorBoxAction Decide(string caption, Func<bool> rollBackScript, Func<bool> leaveMatch)
    {
        if (rollBackScript())
            return ErrorBoxAction.ScriptRolledBack;

        if (caption == WarningTitle)
            return ErrorBoxAction.Continue;

        return leaveMatch() ? ErrorBoxAction.BackToCharacterSelect : ErrorBoxAction.ShowBox;
    }
}
