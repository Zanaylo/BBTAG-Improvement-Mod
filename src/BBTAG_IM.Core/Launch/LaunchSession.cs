using System.Text;

namespace BBTAG_IM.Core.Launch;

public sealed record LaunchSession(string GameDirectory, string LogPath, IReadOnlyList<string> ModFolders)
{
    private const string GameKey = "game";
    private const string LogKey = "log";
    private const string ModKey = "mod";
    private const char KeySeparator = '=';
    private const char LineSeparator = '\n';

    public string Serialize()
    {
        var text = new StringBuilder()
            .Append(GameKey).Append(KeySeparator).Append(GameDirectory).Append(LineSeparator)
            .Append(LogKey).Append(KeySeparator).Append(LogPath).Append(LineSeparator);

        foreach (var modFolder in ModFolders)
            text.Append(ModKey).Append(KeySeparator).Append(modFolder).Append(LineSeparator);

        return text.ToString();
    }

    public static LaunchSession Parse(string text)
    {
        string? gameDirectory = null;
        string? logPath = null;
        var modFolders = new List<string>();

        foreach (var line in text.Split(LineSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf(KeySeparator);

            if (separator < 0)
                continue;

            var value = line[(separator + 1)..];

            switch (line[..separator])
            {
                case GameKey:
                    gameDirectory = value;
                    break;
                case LogKey:
                    logPath = value;
                    break;
                case ModKey:
                    modFolders.Add(value);
                    break;
            }
        }

        if (gameDirectory is null || logPath is null)
            throw new InvalidLaunchSessionException("The launcher did not say where the game and the log are.");

        return new LaunchSession(gameDirectory, logPath, modFolders);
    }
}
