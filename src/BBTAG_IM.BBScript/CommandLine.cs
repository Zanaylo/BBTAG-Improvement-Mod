using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.BBScript;

internal sealed record CommandLine(string Verb, IReadOnlyList<string> Inputs, string? Output, string? Database, bool BBTools, bool Python)
{
    private const string OutputOption = "--out";
    private const string DatabaseOption = "--db";
    private const string BBToolsOption = "--bbtools";
    private const string PythonOption = "--python";

    public static CommandLine? Parse(IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
            return null;

        var inputs = new List<string>();
        string? output = null;
        string? database = null;
        var bbtools = false;
        var python = false;

        for (var index = 1; index < arguments.Count; index++)
        {
            switch (arguments[index])
            {
                case OutputOption when index + 1 < arguments.Count:
                    output = arguments[++index];
                    break;
                case DatabaseOption when index + 1 < arguments.Count:
                    database = arguments[++index];
                    break;
                case BBToolsOption:
                    bbtools = true;
                    break;
                case PythonOption:
                    python = true;
                    break;
                default:
                    inputs.Add(arguments[index]);
                    break;
            }
        }

        return new CommandLine(arguments[0].ToLowerInvariant(), inputs, output, database, bbtools, python);
    }

    public ScriptStyle Style => BBTools ? ScriptStyle.BBTools : ScriptStyle.Readable;

    public ScriptLanguage Language => BBTools || Python ? ScriptLanguage.Python : ScriptLanguage.CSharp;
}
