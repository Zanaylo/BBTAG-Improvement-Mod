using System.Text.Json.Serialization;
using BBTAG_IM.Core.Hitboxes;

namespace BBTAG_IM.Core.Settings;

public sealed class ImprovementSettings
{
    public const string FileName = "ImprovementMod.json";

    public bool FrameLimiter { get; set; }

    public bool HitboxViewer { get; set; } = true;

    public BoxStyle Hurtboxes { get; set; } = new();

    public BoxStyle Hitboxes { get; set; } = new();

    public bool HitboxOrigins { get; set; } = true;

    public string PaletteCreator { get; set; } = "";

    public float? MenuFontSize { get; set; }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, Dictionary<int, string>> Palettes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public BoxStyle StyleOf(BoxKind kind) => kind == BoxKind.Hitbox ? Hitboxes : Hurtboxes;

    public string? PaletteFor(string character, int color) => Palettes.GetValueOrDefault(character)?.GetValueOrDefault(color);

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
