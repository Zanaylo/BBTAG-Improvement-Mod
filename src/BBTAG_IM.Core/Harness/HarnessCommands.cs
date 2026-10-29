namespace BBTAG_IM.Core.Harness;

public static class HarnessCommands
{
    public const string EnvironmentVariable = "BBTAG_IM_HARNESS";
    public const string EnabledValue = "1";
    public const string PipeName = "bbtag-im-harness";

    private const char WordSeparator = ' ';

    public static string Execute(string line, IHarnessTarget target)
    {
        var words = line.Split(WordSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
            return "error empty command";

        return words[0] switch
        {
            "ping" => $"ok {target.ProcessId}",
            "keys" => Keys(words[1..], target),
            "grab" => Grab(line, target),
            "state" => target.State(),
            "back" => target.BackToCharacterSelect(),
            "quit" => target.Quit(),
            _ => $"error unknown command {words[0]}",
        };
    }

    private static string Keys(string[] words, IHarnessTarget target)
    {
        KeyScript script;

        try
        {
            script = KeyScript.Parse(words);
        }
        catch (InvalidKeyScriptException exception)
        {
            return $"error {exception.Message}";
        }

        return target.PlayKeys(script) ? $"ok {script.Steps.Count}" : "error the game stopped taking frames before the keys were played";
    }

    private static string Grab(string line, IHarnessTarget target)
    {
        var path = line.Trim()[4..].Trim();
        return path.Length == 0 ? "error grab needs a path" : target.Grab(path);
    }
}
