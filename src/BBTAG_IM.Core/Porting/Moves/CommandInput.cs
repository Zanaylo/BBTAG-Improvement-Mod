using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Porting.Moves;

public static partial class CommandInput
{
    private const string BothButtons = "B+C";
    private const char NeutralDirection = '0';
    private const string DoubleDownMotion = "202";
    private const string NativeDoubleDown = "22";

    private static readonly Dictionary<char, string> HeldDirections = new()
    {
        ['>'] = "FORWARD",
        ['<'] = "BACK",
        ['v'] = "DOWN",
        ['^'] = "UP",
    };

    public static IReadOnlyList<string>? Requirements(string input)
    {
        if (Motion().Match(input) is { Success: true } motion)
            return WithButton(motion.Groups[1].Value, motion.Groups[2].Value);

        return Held().Match(input) is { Success: true } held ? Direction(held.Groups[1].Value[0], held.Groups[2].Value) : null;
    }

    [GeneratedRegex(@"^(\d*)(A|B|C|D|B\+C)$")]
    private static partial Regex Motion();

    [GeneratedRegex(@"^([0-9><v^])\+(A|B|C|D|B\+C)$")]
    private static partial Regex Held();

    private static IReadOnlyList<string> WithButton(string motion, string button)
    {
        if (motion.Length == 0)
            return [Input("PRESS_" + ButtonName(button))];

        return [Input(motion.EndsWith(DoubleDownMotion, StringComparison.Ordinal) ? NativeDoubleDown : motion), Input("PRESS_" + ButtonName(button))];
    }

    private static IReadOnlyList<string> Direction(char direction, string button)
    {
        if (direction == NeutralDirection)
            return [Input("PRESS_" + ButtonName(button))];

        var held = HeldDirections.TryGetValue(direction, out var group) ? group : direction.ToString();
        return [Input("HOLD_" + held), Input("PRESS_" + ButtonName(button))];
    }

    private static string ButtonName(string button) => button == BothButtons ? "BC" : button;

    private static string Input(string name) => $"Move_Input_(INPUT_{name})";
}
