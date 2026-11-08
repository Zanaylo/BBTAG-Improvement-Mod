using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Porting.Moves;

public static partial class MoveTemplate
{
    private const char LineBreak = '\n';
    private const string RegisterDecorator = "@StateRegister";
    private const string MatchInitStart = "def MatchInit():";
    private const string CommonPrefix = "CmnAct";
    private const string AutoDash = "AutoFDash";
    private const string Indent = "    ";
    private const string NoOperation = "pass";
    private const string CrushTrigger = "CmnActCrushAttack";

    public static string Replace(string template, string registrations, string states, IReadOnlyList<string>? replacedStates = null, IReadOnlyList<string>? stats = null)
    {
        var lines = template.Replace("\r\n", "\n", StringComparison.Ordinal).Split(LineBreak).ToList();
        var moves = RegisteredMoves(lines);
        var kept = DropRegistrations(lines, moves.Append(CrushTrigger).ToHashSet());
        kept = DropStates(kept, moves.Concat(replacedStates ?? []).ToHashSet());
        kept = DropReferences(kept, moves.Except(RegisteredIn(registrations)).ToHashSet());
        var withRegistrations = AddRegistrations(kept, string.Join(LineBreak, (stats ?? []).Append(registrations)));
        return string.Join(LineBreak, withRegistrations).TrimEnd() + "\n\n\n" + states;
    }

    [GeneratedRegex(@"^\s+def (\w+)\(INPUT_\w+\):\s*$")]
    private static partial Regex Registration();

    [GeneratedRegex(@"^def (\w+)\(\):\s*$")]
    private static partial Regex StateStart();

    private static HashSet<string> RegisteredMoves(List<string> lines)
    {
        HashSet<string> moves = [];

        for (var index = 0; index + 1 < lines.Count; index++)
        {
            if (lines[index].Trim() != RegisterDecorator || Registration().Match(lines[index + 1]) is not { Success: true } registered)
                continue;

            if (IsMove(registered.Groups[1].Value))
                moves.Add(registered.Groups[1].Value);
        }

        return moves;
    }

    private static bool IsMove(string name) => !name.StartsWith(CommonPrefix, StringComparison.Ordinal) && name != AutoDash;

    private static List<string> DropRegistrations(List<string> lines, HashSet<string> moves)
    {
        List<string> kept = [];

        for (var index = 0; index < lines.Count; index++)
        {
            var isRegistration = lines[index].Trim() == RegisterDecorator && index + 1 < lines.Count
                && Registration().Match(lines[index + 1]) is { Success: true } registered && moves.Contains(registered.Groups[1].Value);

            if (!isRegistration)
            {
                kept.Add(lines[index]);
                continue;
            }

            index = LastLineOfBlock(lines, index + 1);

            while (index + 1 < lines.Count && lines[index + 1].Trim().Length == 0)
                index++;
        }

        return kept;
    }

    private static int LastLineOfBlock(List<string> lines, int definition)
    {
        var indent = IndentOf(lines[definition]);
        var last = definition;

        for (var index = definition + 1; index < lines.Count; index++)
        {
            if (lines[index].Trim().Length == 0)
                continue;

            if (IndentOf(lines[index]) <= indent)
                break;

            last = index;
        }

        return last;
    }

    private static int IndentOf(string line) => line.Length - line.TrimStart().Length;

    private static List<string> DropStates(List<string> lines, HashSet<string> moves)
    {
        List<string> kept = [];

        for (var index = 0; index < lines.Count; index++)
        {
            var isState = lines[index].StartsWith('@') && index + 1 < lines.Count
                && StateStart().Match(lines[index + 1]) is { Success: true } state && moves.Contains(state.Groups[1].Value);

            if (!isState)
            {
                kept.Add(lines[index]);
                continue;
            }

            index = LastLineOfBlock(lines, index + 1);

            while (index + 1 < lines.Count && lines[index + 1].Trim().Length == 0)
                index++;
        }

        return kept;
    }

    private static IEnumerable<string> RegisteredIn(string registrations) =>
        registrations.Split(LineBreak).Select(line => Registration().Match(Indent + line)).Where(match => match.Success).Select(match => match.Groups[1].Value);

    private static List<string> DropReferences(List<string> lines, HashSet<string> moves)
    {
        if (moves.Count == 0)
            return lines;

        var start = lines.FindIndex(line => line.StartsWith(MatchInitStart, StringComparison.Ordinal));
        var end = start < 0 ? start : LastLineOfBlock(lines, start);
        var pattern = new Regex($@"\b({string.Join('|', moves.Select(Regex.Escape))})\b");
        List<string> kept = [];

        for (var index = 0; index < lines.Count; index++)
        {
            if (!IsStatementAbout(lines[index], pattern))
                kept.Add(lines[index]);
            else if (index > end)
                kept.Add(lines[index][..IndentOf(lines[index])] + NoOperation);
        }

        return kept;
    }

    private static bool IsStatementAbout(string line, Regex pattern) => !line.TrimStart().StartsWith("def ", StringComparison.Ordinal) && pattern.IsMatch(line);

    private static List<string> AddRegistrations(List<string> lines, string registrations)
    {
        if (registrations.Length == 0)
            return lines;

        var start = lines.FindIndex(line => line.StartsWith(MatchInitStart, StringComparison.Ordinal));
        var first = lines.FindIndex(start + 1, line => line.Trim() == RegisterDecorator);
        var at = first >= 0 ? first : LastLineOfBlock(lines, start) + 1;
        var indented = registrations.TrimEnd().Split(LineBreak).Select(line => line.Length == 0 ? line : Indent + line).ToList();
        indented.Add(string.Empty);

        if (first < 0 && lines[at - 1].Trim().Length > 0)
            indented.Insert(0, string.Empty);

        lines.InsertRange(at, indented);
        return lines;
    }
}
