namespace BBTAG_IM.Core.Launch;

public sealed record LaunchOptions(string? GameDirectory, IReadOnlyList<string> ModFolders, bool ShowHelp)
{
    private const string OptionPrefix = "-";

    private static readonly string[] HelpOptions = ["--help", "-h", "/?"];
    private static readonly string[] GameOptions = ["--game", "-g"];
    private static readonly string[] ModOptions = ["--mod", "-m"];

    public static LaunchOptions Parse(IReadOnlyList<string> arguments)
    {
        string? gameDirectory = null;
        var modFolders = new List<string>();
        var showHelp = false;

        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];

            if (HelpOptions.Contains(argument))
            {
                showHelp = true;
                continue;
            }

            if (GameOptions.Contains(argument))
            {
                gameDirectory = ValueAfter(arguments, ref index);
                continue;
            }

            if (ModOptions.Contains(argument))
            {
                modFolders.Add(ValueAfter(arguments, ref index));
                continue;
            }

            if (argument.StartsWith(OptionPrefix, StringComparison.Ordinal))
                throw new LaunchOptionsException($"Unknown option {argument}. Run with --help to see the options.");

            modFolders.Add(argument);
        }

        return new LaunchOptions(gameDirectory, modFolders, showHelp);
    }

    private static string ValueAfter(IReadOnlyList<string> arguments, ref int index)
    {
        var option = arguments[index];

        if (++index >= arguments.Count)
            throw new LaunchOptionsException($"{option} needs a folder after it.");

        return arguments[index];
    }
}
