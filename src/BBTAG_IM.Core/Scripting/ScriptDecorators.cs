namespace BBTAG_IM.Core.Scripting;

public static class ScriptDecorators
{
    public const string State = "State";
    public const string Subroutine = "Subroutine";
    public const string RawState = "RawState";
    public const string RawSubroutine = "RawSubroutine";
    public const string StateRegister = "StateRegister";

    public static string For(int startCommand, bool raw) => (startCommand, raw) switch
    {
        (ScriptCommands.StartState, false) => State,
        (ScriptCommands.StartState, true) => RawState,
        (_, false) => Subroutine,
        _ => RawSubroutine,
    };
}
