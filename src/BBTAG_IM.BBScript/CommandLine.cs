using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.BBScript;

internal sealed record CommandLine(string Verb, IReadOnlyList<string> Inputs, string? Output, string? Database, bool BBTools, bool Python, bool Foreign, string? Roster, string? Name, string? Series)
{
    private const string OutputOption = "--out";
    private const string DatabaseOption = "--db";
    private const string BBToolsOption = "--bbtools";
    private const string PythonOption = "--python";
    private const string ForeignOption = "--foreign";
    private const string RosterOption = "--roster";
    private const string NameOption = "--name";
    private const string SeriesOption = "--series";

    public static CommandLine? Parse(IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
            return null;

        var inputs = new List<string>();
        string? output = null;
        string? database = null;
        var bbtools = false;
        var python = false;
        var foreign = false;
        string? roster = null;
        string? name = null;
        string? series = null;

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
                case RosterOption when index + 1 < arguments.Count:
                    roster = arguments[++index];
                    break;
                case NameOption when index + 1 < arguments.Count:
                    name = arguments[++index];
                    break;
                case SeriesOption when index + 1 < arguments.Count:
                    series = arguments[++index];
                    break;
                case BBToolsOption:
                    bbtools = true;
                    break;
                case PythonOption:
                    python = true;
                    break;
                case ForeignOption:
                    foreign = true;
                    break;
                default:
                    inputs.Add(arguments[index]);
                    break;
            }
        }

        return new CommandLine(arguments[0].ToLowerInvariant(), inputs, output, database, bbtools, python, foreign, roster, name, series);
    }

    public ScriptStyle Style => BBTools ? ScriptStyle.BBTools : ScriptStyle.Readable;

    public ScriptLanguage Language => BBTools || Python ? ScriptLanguage.Python : ScriptLanguage.CSharp;
}
