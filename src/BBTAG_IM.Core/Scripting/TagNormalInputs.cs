using System.Globalization;

namespace BBTAG_IM.Core.Scripting;

public sealed class TagNormalInputs : INormalInputs
{
    private const int FirstButtonInput = 3;
    private const int DirectionsPerButton = 18;
    private const int GroundDirections = 9;
    private const string AirMark = "J";

    private static readonly string[] Buttons = ["A", "B", "C", "D", "E"];

    private static readonly NamedValue[] SystemInputs =
    [
        new(0x02, "INPUT_SPECIALMOVE", "A special move: its motion and button are given by the Move_Input_ lines below it."),
        new(0x5D, "INPUT_THROW", "Throw (NmlAtkThrow)."),
        new(0x61, "INPUT_BACKTHROW", "Back throw (NmlAtkBackThrow)."),
        new(0x63, "INPUT_CHANGE_PARTNER", "Partner change (CmnActChangePartnerQuickOut)."),
        new(0x64, "INPUT_REVERSAL", "Reversal Action on the ground (CmnActInvincibleAttack)."),
        new(0x65, "INPUT_AIR_REVERSAL", "Reversal Action in the air (CmnActInvincibleAttackAir)."),
        new(0x66, "INPUT_CRUSH_TRIGGER", "Crush Trigger (CmnActCrushAttack)."),
        new(0x68, "INPUT_DISTORTION", "A Distortion Skill: its motion is given by the Move_Input_ lines below it."),
        new(0x69, "INPUT_ASTRAL", "Astral Heat."),
    ];

    public static readonly TagNormalInputs Instance = new();

    private readonly NameTable table = NameTable.Of(SystemInputs.Concat(ButtonInputs()));

    private TagNormalInputs()
    {
    }

    public IEnumerable<NamedValue> Named => table.Named;

    public string? NameOf(int value) => table.NameOf(value);

    public int? ValueOf(string name) => table.ValueOf(name);

    private static IEnumerable<NamedValue> ButtonInputs()
    {
        for (var button = 0; button < Buttons.Length; button++)
        {
            for (var direction = 0; direction < DirectionsPerButton; direction++)
                yield return ButtonInput(button, direction);
        }
    }

    private static NamedValue ButtonInput(int button, int direction)
    {
        var inAir = direction >= GroundDirections;
        var numpad = (direction % GroundDirections + 1).ToString(CultureInfo.InvariantCulture);
        var value = FirstButtonInput + button * DirectionsPerButton + direction;
        var name = InputNames.Prefix + (inAir ? AirMark : string.Empty) + numpad + Buttons[button];
        var description = inAir ? $"j.{numpad}{Buttons[button]}: {numpad}{Buttons[button]} in the air." : $"{numpad}{Buttons[button]} on the ground.";

        return new NamedValue(value, name, description);
    }
}
