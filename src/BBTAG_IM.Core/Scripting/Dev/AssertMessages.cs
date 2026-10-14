using System.Globalization;
using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Scripting.Dev;

public static partial class AssertMessages
{
    private const string AssertTitle = "BB_Assert";
    private const string FileLine = "file -";
    private const string LineLine = "line -";

    public static string Describe(string raw)
    {
        var text = string.Join(' ', raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line != AssertTitle && !line.StartsWith(FileLine, StringComparison.Ordinal) && !line.StartsWith(LineLine, StringComparison.Ordinal)));

        if (MissingLabel().Match(text) is { Success: true } label)
            return $"This jumps to label {Number(label)}, which does not exist in this state.";

        if (UnregisteredCommand().Match(text) is { Success: true } command)
            return $"Command {Number(command)} is not one the game knows.";

        if (MissingState().Match(text) is { Success: true } state)
            return $"There is no state named {state.Groups[1].Value}, or it is misspelled.";

        if (NestedJump().IsMatch(text))
            return "A sprite or a jump sits inside a nested block (if, upon...). Use ReqGoto inside nested blocks.";

        if (FunctionCall().Match(text) is { Success: true } call)
            return $"The subroutine {call.Groups[1].Value} could not be called.";

        return text;
    }

    private static int Number(Match match) => int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    [GeneratedRegex("Label 0x([0-9a-fA-F]+) にGoto")]
    private static partial Regex MissingLabel();

    [GeneratedRegex("0x([0-9a-fA-F]+)は登録されていないコマンドID")]
    private static partial Regex UnregisteredCommand();

    [GeneratedRegex("つづりを間違えています。(\\S+)")]
    private static partial Regex MissingState();

    [GeneratedRegex("CellBeginまたはGotoが不正な位置")]
    private static partial Regex NestedJump();

    [GeneratedRegex("Script Function Call Error <<(.+)>>")]
    private static partial Regex FunctionCall();
}
