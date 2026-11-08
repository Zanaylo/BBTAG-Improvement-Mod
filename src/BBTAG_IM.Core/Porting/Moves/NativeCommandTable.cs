using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Porting.Moves;

public sealed record NativeCommand(string Name, IReadOnlyList<string> Inputs, bool InAir, int Cost, string? MoveName, IReadOnlyList<string> OnlyFrom);

public sealed partial class NativeCommandTable
{
    private const string EntryMark = "\nt.";
    private const string CommentMark = "//";
    private const string ReverseBeatOn = "1";

    private readonly Dictionary<string, NativeCommand> _commands;
    private readonly Dictionary<string, string> _constants;

    private NativeCommandTable(Dictionary<string, NativeCommand> commands, Dictionary<string, string> constants, bool hasReverseBeat)
    {
        _commands = commands;
        _constants = constants;
        HasReverseBeat = hasReverseBeat;
    }

    public bool HasReverseBeat { get; }

    public static NativeCommandTable Empty { get; } = new([], [], false);

    public static NativeCommandTable Parse(string table, string defines)
    {
        var constants = Constants(defines);
        Dictionary<string, NativeCommand> commands = [];
        var lines = WithoutComments(table.Replace("\r\n", "\n", StringComparison.Ordinal));

        foreach (var entry in ("\n" + lines).Split(EntryMark).Skip(1))
        {
            if (Entry().Match(entry) is { Success: true } start && Read(start.Groups[1].Value, entry, constants) is { } command)
                commands[command.Name] = command;
        }

        return new NativeCommandTable(commands, constants, ReverseBeat().Match(defines) is { Success: true } rule && rule.Groups[1].Value == ReverseBeatOn);
    }

    public NativeCommand? Find(string name) => _commands.GetValueOrDefault(name);

    public NativeCommandTable WithCharacter(string commands)
    {
        var text = WithoutComments(commands.Replace("\r\n", "\n", StringComparison.Ordinal));
        Dictionary<string, NativeCommand> merged = new(_commands);

        foreach (Match entry in CharacterEntry().Matches(text))
        {
            if (Read(entry.Groups[1].Value, Block(text, entry.Index + entry.Length - 1), _constants) is { } command)
                merged[command.Name] = command;
        }

        return new NativeCommandTable(merged, _constants, HasReverseBeat);
    }

    private static string Block(string text, int open)
    {
        var depth = 0;

        for (var index = open; index < text.Length; index++)
        {
            if (text[index] == '{')
                depth++;

            if (text[index] == '}' && --depth == 0)
                return text[open..(index + 1)];
        }

        return text[open..];
    }

    [GeneratedRegex(@"^\s*const\s+(Def_CMD_\w+)\s*=\s*""([^""]*)""", RegexOptions.Multiline)]
    private static partial Regex Constant();

    [GeneratedRegex(@"^(\w+)\s*<-\s*\n?\s*\{")]
    private static partial Regex Entry();

    [GeneratedRegex(@"(?:^\s*|\bcmd\.)((?:Skill|Atk|Throw)_\w+)\s*(?:=|<-)\s*\n?\s*\{", RegexOptions.Multiline)]
    private static partial Regex CharacterEntry();

    [GeneratedRegex(@"\bcommand\s*=\s*(\[[^\]]*\]|""[^""]*""|Def_CMD_\w+)")]
    private static partial Regex Command();

    [GeneratedRegex(@"""([^""]*)""|(Def_CMD_\w+)")]
    private static partial Regex CommandItem();

    [GeneratedRegex(@"@[^@]*@")]
    private static partial Regex Timing();

    [GeneratedRegex(@"\busegauge\s*=\s*(\d+)")]
    private static partial Regex Gauge();

    [GeneratedRegex(@"\bOnly\s*=\s*(\[[^\]]*\]|""[^""]*"")")]
    private static partial Regex OnlyFrom();

    [GeneratedRegex(@"UpdateTable\s*=\s*\{\s*name\s*=\s*""(\w+)""")]
    private static partial Regex MoveTable();

    [GeneratedRegex(@"""(\w+)""")]
    private static partial Regex Quoted();

    [GeneratedRegex(@"const\s+Def_Rule_EnableReverceBeat\s*=\s*(\d+)")]
    private static partial Regex ReverseBeat();

    private static Dictionary<string, string> Constants(string defines)
    {
        Dictionary<string, string> constants = [];

        foreach (Match constant in Constant().Matches(defines))
            constants.TryAdd(constant.Groups[1].Value, constant.Groups[2].Value);

        return constants;
    }

    private static string WithoutComments(string table) =>
        string.Join('\n', table.Split('\n').Select(line => line.IndexOf(CommentMark, StringComparison.Ordinal) is var comment and >= 0 ? line[..comment] : line));

    private static NativeCommand? Read(string name, string entry, Dictionary<string, string> constants)
    {
        if (Command().Match(entry) is not { Success: true } command)
            return null;

        var inputs = command.Groups[1].Value.Trim('[', ']').Split(',').Select(item => Joined(item, constants)).Where(input => input.Length > 0).ToArray();

        return new NativeCommand(
            name,
            inputs,
            entry.Contains("_PosState_Air", StringComparison.Ordinal),
            Gauge().Match(entry) is { Success: true } gauge ? int.Parse(gauge.Groups[1].Value) : 0,
            MoveTable().Match(entry) is { Success: true } move ? move.Groups[1].Value : null,
            OnlyFrom().Match(entry) is { Success: true } only ? Quoted().Matches(only.Groups[1].Value).Select(item => item.Groups[1].Value).ToArray() : []);
    }

    private static string Joined(string item, Dictionary<string, string> constants) =>
        string.Concat(CommandItem().Matches(item).Select(part => Text(part, constants)));

    private static string Text(Match item, Dictionary<string, string> constants)
    {
        var text = item.Groups[1].Success ? item.Groups[1].Value : constants.GetValueOrDefault(item.Groups[2].Value, string.Empty);
        return Timing().Replace(text, string.Empty);
    }
}
