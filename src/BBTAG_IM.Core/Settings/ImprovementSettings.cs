using System.Text.Json.Serialization;
using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Settings;

public sealed class ImprovementSettings
{
    public const string FileName = "ImprovementMod.json";

    public bool FrameLimiter { get; set; }

    public bool AdvancedMapperButtons { get; set; } = true;

    public bool HitboxViewer { get; set; } = true;

    public BoxStyle Hurtboxes { get; set; } = new();

    public BoxStyle Hitboxes { get; set; } = new();

    public bool HitboxOrigins { get; set; } = true;

    public FrameMeterOptions FrameMeter { get; set; } = new();

    public string PaletteCreator { get; set; } = "";

    public float? MenuFontSize { get; set; }

    public float? PaletteDetailWidth { get; set; }

    public string? ScriptEditor { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<ScriptLanguage>))]
    public ScriptLanguage ScriptLanguage { get; set; } = ScriptLanguage.CSharp;

    [JsonConverter(typeof(JsonStringEnumConverter<ScriptStyle>))]
    public ScriptStyle PythonLayout { get; set; } = ScriptStyle.Mango;

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, Dictionary<int, string>> Palettes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public BoxStyle StyleOf(BoxKind kind) => kind == BoxKind.Hitbox ? Hitboxes : Hurtboxes;

    public string? PaletteFor(string character, int color) => Palettes.GetValueOrDefault(character)?.GetValueOrDefault(color);

    public int HighestColor(string character)
    {
        if (!Palettes.TryGetValue(character, out var colors))
            return 0;

        var highest = 0;

        foreach (var color in colors.Keys)
            highest = Math.Max(highest, color);

        return highest;
    }

    public int? ColorOf(string character, string palette)
    {
        if (!Palettes.TryGetValue(character, out var colors))
            return null;

        int? lowest = null;

        foreach (var (color, name) in colors)
        {
            if (name.Equals(palette, StringComparison.OrdinalIgnoreCase) && (lowest is null || color < lowest))
                lowest = color;
        }

        return lowest;
    }

    public void AssignPalette(string character, string palette, int? color)
    {
        while (ColorOf(character, palette) is { } held)
            ChoosePalette(character, held, null);

        if (color is { } chosen)
            ChoosePalette(character, chosen, palette);
    }

    public void ChoosePalette(string character, int color, string? palette)
    {
        if (palette is not null)
        {
            Palettes.TryAdd(character, []);
            Palettes[character][color] = palette;
            return;
        }

        if (!Palettes.TryGetValue(character, out var colors))
            return;

        colors.Remove(color);

        if (colors.Count == 0)
            Palettes.Remove(character);
    }
}
