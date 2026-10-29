using System.Globalization;

namespace BBTAG_IM.Core.Scripting;

public static class TagMoveConditions
{
    private const int FirstSlotCheck = 0x3000;
    private const int FirstSlotZeroCheck = 0x3008;
    private const int FirstCheckedSlot = 4;
    private const int CheckedSlots = 8;
    private const int FirstSlotFourLowValue = 0x3025;
    private const int SlotFourLowValues = 9;
    private const int FirstSlotFourHighValue = 0x3074;
    private const int FirstHighValue = 10;
    private const int SlotFourHighValues = 11;
    private const int FirstSectionCheck = 0x302E;
    private const int FirstEmptyCheck = 0x303C;
    private const int FirstNotEmptyCheck = 0x304D;
    private const int CharacterGauges = 5;
    private const int FirstHeightCheck = 0x3036;
    private const int HeightStep = 50000;
    private const int HeightChecks = 4;
    private const int FirstGaugeStep = 0x3053;
    private const int GaugeStep = 1250;
    private const int GaugeSteps = 8;
    private const int FirstSlotTwelveBit = 0x305B;
    private const int SlotTwelveBits = 16;
    private const string GameCode = " (BBTAG.exe move condition check at 0x7BEA10.)";

    private static readonly NamedValue[] Conditions =
    [
        new(0x2000, "MOVE_GROUND", "The character is on the ground."),
        new(0x2001, "MOVE_AIR", "The character is in the air."),
        new(0x2002, "MOVE_SKILL_GAUGE_5000", "The team's Skill Gauge is 5000 or more."),
        new(0x2003, "MOVE_SKILL_GAUGE_10000", "The team's Skill Gauge is 10000 or more."),
        new(0x2004, "MOVE_SKILL_GAUGE_2500", "The team's Skill Gauge is 2500 or more."),
        new(0x2006, "MOVE_0x2006", "On the ground, or the character value at +0x2920 is above 0. Debug option 40 makes it always true."),
        new(0x2007, "MOVE_0x2007", "The character value at +0x2924 is above 0. Debug option 40 makes it always true. Every air dash registration uses it (AirDash_66_ButtonInput, SpecialDashAirF...)."),
        new(0x200B, "MOVE_0x200B", "Bit 0x200 of the character flags at +0x28FC is set."),
        new(0x3024, "MOVE_0x3024", "SLOT_4 is 0: the same check as MOVE_UNLESS_SLOT_4."),
        new(0x303A, "MOVE_0x303A", "One of the bits 0xC00000 of the object flags at +0x158 is set."),
        new(0x303B, "MOVE_0x303B", "One of the bits 0xF00000 of the object flags at +0x158 is set."),
        new(0x3048, "MOVE_NEAR_LEFT_WALL", "The character's left side is less than 50000 from the stage's left limit."),
        new(0x3049, "MOVE_NEAR_RIGHT_WALL", "The character's right side is less than 50000 from the stage's right limit."),
        new(0x304A, "MOVE_ASTRAL", "The game's Astral Heat check (0x7B8780): the team is one round from winning the match, plus the game's other Astral Heat rules. Every Astral Heat uses it."),
        new(0x304B, "MOVE_0x304B", "Function 0x7B8630 on the character returns 1 or more."),
        new(0x304C, "MOVE_0x304C", "Bit 20 of the character flags at +0x28FC is clear."),
        new(0x3052, "MOVE_0x3052", "The height above the ground is at least the character value at +0x586C."),
        new(0x306B, "MOVE_0x306B", "Function 0x67D0F0 on the team returns non-zero."),
        new(0x306C, "MOVE_0x306C", "Function 0x67D0F0 on the team returns 0."),
        new(0x306D, "MOVE_0x306D", "The game mode value at +0xA4898 is 3."),
        new(0x306E, "MOVE_0x306E", "Function 0x7A7380 on the character returns 0."),
        new(0x306F, "MOVE_DEBUG", "Debug option 100 is on. It is off in the released game, so the move never shows up there. The DebugSkill and DS_ states use it."),
        new(0x3070, "MOVE_0x3070", "The opponent is in the air, or bit 2 of its flags at +0x28FC is set."),
        new(0x3071, "MOVE_0x3071", "Function 0x7653F0 on the character returns non-zero (game mode 4 with the team's flag set)."),
        new(0x3072, "MOVE_0x3072", "Function 0x7653F0 on the character returns 0: the opposite of MOVE_0x3071."),
        new(0x3073, "MOVE_0x3073", "Bit 1 of the opponent's flags at +0x28FC is set."),
        new(0x307F, "MOVE_0x307F", "Bit 0x200000 of the opponent's flags at +0x26C is set."),
        new(0x3080, "MOVE_0x3080", "Function 0x764B60 gives a higher value for the opponent than for the character."),
        new(0x3081, "MOVE_OD", "The team's flag at +0x30 or +0x48 is 1. The game's own _OD and SP versions of Distortion Skills use it."),
        new(0x3082, "MOVE_0x3082", "The character value at +0x28AD0 is not 0."),
        new(0x3083, "MOVE_0x3083", "Function 0x7C5230 on the character returns non-zero. Only the 12 Persona 4 Arena characters use it (on their 5B, j.5B, j.5C, Reversal Actions and Astral Heats)."),
        new(0x3084, "MOVE_0x3084", "The character value at +0x2788 is 0 or less."),
        new(0x3085, "MOVE_0x3085", "The team's Skill Gauge is 5000 or more: the same check as MOVE_EX."),
        new(0x3086, "MOVE_EX", "The team's Skill Gauge is 5000 or more. Every EX move uses it."),
        new(0x3087, "MOVE_0x3087", "The team's Skill Gauge is 5000 or more: the same check as MOVE_EX."),
        new(0x3088, "MOVE_0x3088", "The team's Skill Gauge is 10000 or more: the same check as MOVE_DISTORTION."),
        new(0x3089, "MOVE_DISTORTION", "The team's Skill Gauge is 10000 or more. Every Distortion Skill uses it."),
        new(0x308A, "MOVE_0x308A", "The team's Skill Gauge is 10000 or more: the same check as MOVE_DISTORTION."),
        new(0x308B, "MOVE_SKILL_GAUGE_15000", "The team's Skill Gauge is 15000 or more."),
        new(0x308C, "MOVE_SKILL_GAUGE_45000", "The team's Skill Gauge is 45000 or more."),
        new(0x308D, "MOVE_0x308D", "Function 0x7B9020 on the character returns non-zero."),
    ];

    public static NameTable Table { get; } = NameTable.Of(
        Conditions
            .Concat(SlotChecks())
            .Concat(SlotFourValues())
            .Concat(GaugeChecks())
            .Concat(HeightChecksList())
            .Concat(GaugeStepChecks())
            .Concat(SlotTwelveBitChecks())
            .Select(condition => condition with { Description = condition.Description + GameCode }));

    private static IEnumerable<NamedValue> SlotChecks()
    {
        for (var index = 0; index < CheckedSlots; index++)
        {
            var slot = Number(FirstCheckedSlot + index);
            yield return new NamedValue(FirstSlotCheck + index, "MOVE_IF_SLOT_" + slot, $"SLOT_{slot} is not 0.");
            yield return new NamedValue(FirstSlotZeroCheck + index, "MOVE_UNLESS_SLOT_" + slot, $"SLOT_{slot} is 0.");
        }
    }

    private static IEnumerable<NamedValue> SlotFourValues()
    {
        for (var value = 1; value <= SlotFourLowValues; value++)
            yield return SlotFourIs(FirstSlotFourLowValue + value - 1, value);

        for (var index = 0; index < SlotFourHighValues; index++)
            yield return SlotFourIs(FirstSlotFourHighValue + index, FirstHighValue + index);
    }

    private static NamedValue SlotFourIs(int condition, int value) =>
        new(condition, $"MOVE_IF_SLOT_4_IS_{Number(value)}", $"SLOT_4 is {Number(value)}.");

    private static IEnumerable<NamedValue> GaugeChecks()
    {
        for (var gauge = 0; gauge < CharacterGauges; gauge++)
        {
            var number = Number(gauge);
            var gaugeText = $"Character gauge {number} (the one Unknown23003({number}, ...) sets up)";
            yield return new NamedValue(FirstSectionCheck + gauge, $"MOVE_GAUGE_{number}_SECTION", $"{gaugeText} has at least one full section.");
            yield return new NamedValue(FirstEmptyCheck + gauge, $"MOVE_GAUGE_{number}_EMPTY", $"{gaugeText} is 0 or less.");
            yield return new NamedValue(FirstNotEmptyCheck + gauge, $"MOVE_GAUGE_{number}_NOT_EMPTY", $"{gaugeText} is not 0.");
        }
    }

    private static IEnumerable<NamedValue> HeightChecksList()
    {
        for (var index = 0; index < HeightChecks; index++)
        {
            var height = Number(HeightStep * (index + 1));
            yield return new NamedValue(FirstHeightCheck + index, "MOVE_HEIGHT_" + height, $"The height above the ground is {height} or more.");
        }
    }

    private static IEnumerable<NamedValue> GaugeStepChecks()
    {
        for (var step = 1; step <= GaugeSteps; step++)
        {
            var amount = Number(GaugeStep * step);
            yield return new NamedValue(
                FirstGaugeStep + step - 1,
                $"MOVE_SKILL_GAUGE_STEP_{Number(step)}",
                $"The team's Skill Gauge is {amount} or more, or the character value at +0x241E0 is above 0.");
        }
    }

    private static IEnumerable<NamedValue> SlotTwelveBitChecks()
    {
        for (var bit = 0; bit < SlotTwelveBits; bit++)
            yield return new NamedValue(FirstSlotTwelveBit + bit, $"MOVE_IF_SLOT_12_BIT_{Number(bit)}", $"Bit {Number(bit)} (0x{1 << bit:X}) of SLOT_12 is set.");
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
