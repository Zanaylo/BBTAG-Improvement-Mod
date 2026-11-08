using System.Globalization;

namespace BBTAG_IM.Core.Scripting;

public static class TagMoveInputs
{
    private const int FirstDirection = 0x37;
    private const int ValuesPerDirection = 13;
    private const int HoldAnyOffset = 1;
    private const int NotHoldOffset = 8;
    private const int NotHoldAnyOffset = 9;
    private const int ValuesPerButton = 9;
    private const int FirstButton = 1;
    private const int PressNowOffset = 1;
    private const int NotHoldButtonOffset = 4;
    private const int NumpadDirections = 9;
    private const string SheetSource = " Name from the community Command Database sheet; the BBTAG.exe code that sets it is not mapped.";
    private const string NoInputSource = " With no input at all BBTAG.exe sets every one of these (0x7C0160).";
    private const string HeldDirectionSource = " BBTAG.exe reads these to find the held direction (0x7BFB40).";
    private const string ButtonSource = " BBTAG.exe sets it while the button is not held (0x7BAE90).";

    private static readonly string[] Buttons = ["A", "B", "C", "D", "E"];

    private static readonly Dictionary<int, string> DirectionGroups = new()
    {
        [1] = "DOWN_OR_BACK",
        [2] = "DOWN",
        [3] = "DOWN_OR_FORWARD",
        [4] = "BACK",
        [6] = "FORWARD",
        [7] = "UP_OR_BACK",
        [8] = "UP",
        [9] = "UP_OR_FORWARD",
    };

    private static readonly NamedValue[] Others =
    [
        new(37, "INPUT_HOLD_E", "E is held. The fifth battle button: BBTAG.exe never sets it, the Improvement Mod does when Advanced mapper buttons is on."),
        new(39, "INPUT_RELEASE_E", "E was released."),
        new(40, "INPUT_PRESS_E", "E was pressed. BBTAG.exe tests this for INPUT_1E to INPUT_J9E (0x786A80)."),
        new(0xDE, "INPUT_PRESS_BC", "B and C pressed together: every Distortion Skill registers 236 or 214 with it. Name from the mod author's notes."),
        new(0xE2, "INPUT_DASH_22", "The 22 of the game's second dashes (__2ndDash22, AirDash_22, Swoop). INPUT_22 is another value."),
        new(0xE4, "INPUT_DASH_44", "The 44 of the game's second dashes (__2ndDash44). INPUT_44 is another value."),
        new(0xE5, "INPUT_DASH_66", "The 66 of the game's second dashes (__2ndDash66, DashCancel). INPUT_66 is another value."),
        new(0xE7, "INPUT_DASH_88", "The 88 of the game's second dashes (__2ndDash88)."),
    ];

    public static NameTable Table { get; } = NameTable.Of(ButtonInputs().Concat(DirectionInputs()).Concat(Others));

    private static IEnumerable<NamedValue> ButtonInputs()
    {
        for (var index = 0; index < Buttons.Length; index++)
        {
            var button = Buttons[index];
            var first = FirstButton + index * ValuesPerButton;

            yield return new NamedValue(first + PressNowOffset, $"INPUT_PRESS_NOW_{button}", $"{button} was pressed on this frame. INPUT_PRESS_{button} also takes a press up to 5 frames old.{SheetSource}");
            yield return new NamedValue(first + NotHoldButtonOffset, $"INPUT_NOT_HOLD_{button}", $"{button} is not held.{ButtonSource}");
        }
    }

    private static IEnumerable<NamedValue> DirectionInputs()
    {
        for (var numpad = 1; numpad <= NumpadDirections; numpad++)
        {
            var first = FirstDirection + (numpad - 1) * ValuesPerDirection;
            var digit = numpad.ToString(CultureInfo.InvariantCulture);

            yield return new NamedValue(first, "INPUT_HOLD_" + digit, $"Holding {digit} (only {digit}).{HeldDirectionSource}");
            yield return new NamedValue(first + NotHoldOffset, "INPUT_NOT_HOLD_" + digit, $"Not holding {digit}.{NoInputSource}");

            if (!DirectionGroups.TryGetValue(numpad, out var group))
                continue;

            var words = group.Replace('_', ' ').ToLowerInvariant();
            yield return new NamedValue(first + HoldAnyOffset, "INPUT_HOLD_" + group, $"Holding {words}: any direction that has it ({digit} included).{SheetSource}");
            yield return new NamedValue(first + NotHoldAnyOffset, "INPUT_NOT_HOLD_" + group, $"Not holding {words}.{NoInputSource}");
        }
    }
}
