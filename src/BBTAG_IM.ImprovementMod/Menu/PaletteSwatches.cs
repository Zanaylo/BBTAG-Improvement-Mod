using System.Numerics;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.Core.Palettes;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal static class PaletteSwatches
{
    private const int CharacterFile = 0;
    private const int Stripes = 8;
    private const int FirstStripeEntry = 1;
    private const int StripeEntryStep = 12;
    private const int ColorsPerRow = 16;
    private const float Gap = 1;

    public static void DrawStripes(ImDrawListPtr draw, Vector2 topLeft, Vector2 size, ImplPalette palette)
    {
        var height = size.Y / Stripes;

        for (var stripe = 0; stripe < Stripes; stripe++)
        {
            var color = palette.ColorAt(CharacterFile, FirstStripeEntry + stripe * StripeEntryStep);
            var top = topLeft + new Vector2(0, stripe * height);
            draw.AddRectFilled(top, top + new Vector2(size.X, height), OverlayColors.Swatch(color));
        }
    }

    public static void DrawAllColors(ImplPalette palette, float width)
    {
        var side = (width - Gap * (ColorsPerRow - 1)) / ColorsPerRow;

        if (side <= 0)
            return;

        var draw = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var rows = PaletteColors.ColorsPerFile / ColorsPerRow;

        for (var entry = 0; entry < PaletteColors.ColorsPerFile; entry++)
        {
            var cell = origin + new Vector2(entry % ColorsPerRow, entry / ColorsPerRow) * (side + Gap);
            draw.AddRectFilled(cell, cell + new Vector2(side), OverlayColors.Swatch(palette.ColorAt(CharacterFile, entry)));
        }

        ImGui.Dummy(new Vector2(width, rows * (side + Gap)));
    }
}
