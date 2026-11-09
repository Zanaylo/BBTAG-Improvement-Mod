using BBTAG_IM.Core.Overlay;

namespace BBTAG_IM.Core.Meter;

public static class MeterLayout
{
    public const int VisibleFrames = 100;

    private const float DesignHeight = 1080;
    private const float Column = 10;
    private const float CellWidth = 9;
    private const float PointHeight = 18;
    private const float PartnerHeight = 9;
    private const float StripHeight = 4;
    private const float StripGap = 1;
    private const float LaneGap = 2;
    private const float HeaderHeight = 22;
    private const float PartnerLineHeight = 16;
    private const float CenterHeight = 18;
    private const float CountHeight = 13;
    private const float Padding = 8;
    private const float ChipWidth = 34;
    private const float BlockGap = 3;
    private const float CenterGap = 5;
    private const float ItemGap = 10;
    private const float LabelGap = 4;
    private const float EdgeHeight = 2;
    private const float MarkSize = 3;
    private const float HitStartWidth = 1;
    private const float BottomShare = 0.86f;
    private const float TrackWidth = VisibleFrames * Column;
    private const float PanelWidth = Padding + ChipWidth + TrackWidth + Padding;
    private const int ShortestCountedRun = 2;

    private const string AdvantageLabel = "ADVANTAGE";
    private const string GapLabel = "GAP";
    private const string FreezeLabel = "FREEZE";
    private const string TotalLabel = "TOTAL";

    private static readonly string[] TimingLabels = ["STARTUP", "ACTIVE", "RECOVERY", TotalLabel];

    public static void Draw(HudScene scene, MeterView view, MeterLook look, float screenWidth, float screenHeight, IHudFont font)
    {
        var size = screenHeight / DesignHeight * look.Scale;
        var rows = Rows(view, look);
        var height = rows.Sum(row => row.Height);
        var left = Math.Clamp((screenWidth - PanelWidth * size) / 2 + look.OffsetX * screenWidth, 0, Math.Max(0, screenWidth - PanelWidth * size));
        var top = Math.Clamp(screenHeight * BottomShare + look.OffsetY * screenHeight - height * size, 0, Math.Max(0, screenHeight - height * size));
        var painter = new Painter(scene, font, look.Opacity, size, left, top);

        painter.Fill(0, 0, PanelWidth, height, MeterColors.Panel);
        painter.Fill(0, 0, PanelWidth, EdgeHeight, MeterColors.PanelEdge);
        painter.Fill(0, height - EdgeHeight, PanelWidth, EdgeHeight, MeterColors.PanelEdge);

        var y = 0f;

        foreach (var row in rows)
        {
            DrawRow(painter, view, look, row, y);
            y += row.Height;
        }
    }

    private static List<Row> Rows(MeterView view, MeterLook look)
    {
        var rows = new List<Row> { new(RowKind.Space, Side.Player1, Padding) };
        rows.AddRange(TopBlock(view.Player1, look));
        rows.Add(new Row(RowKind.Space, Side.Player1, CenterGap));

        if (look.Totals)
            rows.Add(new Row(RowKind.Center, Side.Player1, CenterHeight));

        rows.Add(new Row(RowKind.Space, Side.Player1, CenterGap));
        rows.AddRange(Enumerable.Reverse(TopBlock(view.Player2, look)).Select(row => row with { Side = Side.Player2 }));
        rows.Add(new Row(RowKind.Space, Side.Player2, Padding));
        return rows;
    }

    private static List<Row> TopBlock(SideView side, MeterLook look)
    {
        var rows = new List<Row> { new(RowKind.Header, Side.Player1, HeaderHeight) };

        if (ShowsPartnerLine(side, look))
            rows.Add(new Row(RowKind.PartnerLine, Side.Player1, PartnerLineHeight));

        rows.Add(new Row(RowKind.Space, Side.Player1, BlockGap));

        if (look.Invulnerability)
        {
            rows.Add(new Row(RowKind.Strip, Side.Player1, StripHeight));
            rows.Add(new Row(RowKind.Space, Side.Player1, StripGap));
        }

        rows.Add(new Row(RowKind.PointLane, Side.Player1, PointHeight));

        if (look.Partners)
        {
            rows.Add(new Row(RowKind.Space, Side.Player1, LaneGap));
            rows.Add(new Row(RowKind.PartnerLane, Side.Player1, PartnerHeight));
        }

        return rows;
    }

    private static bool ShowsPartnerLine(SideView side, MeterLook look) =>
        look.Partners && side.PartnerMove is not null && Window(side.Partner).Any(cell => cell.Kind != FrameKind.Empty);

    private static void DrawRow(Painter painter, MeterView view, MeterLook look, Row row, float y)
    {
        var side = row.Side == Side.Player1 ? view.Player1 : view.Player2;

        switch (row.Kind)
        {
            case RowKind.Header:
                DrawHeader(painter, side, row.Side, y);
                break;
            case RowKind.PartnerLine:
                DrawTiming(painter, side.PartnerName, side.PartnerMove, Padding + ChipWidth, y, PartnerLineHeight);
                break;
            case RowKind.Strip:
                DrawStrip(painter, Window(side.Point), y);
                break;
            case RowKind.PointLane:
                DrawLane(painter, Window(side.Point), y, PointHeight);
                DrawCounts(painter, Window(side.Point), y, look.Counts);
                break;
            case RowKind.PartnerLane:
                DrawLane(painter, Window(side.Partner), y, PartnerHeight);
                break;
            case RowKind.Center:
                DrawCenter(painter, view, y);
                break;
        }
    }

    private static void DrawHeader(Painter painter, SideView side, Side which, float y)
    {
        painter.Write(which == Side.Player1 ? "1P" : "2P", Padding, y, HeaderHeight, which == Side.Player1 ? MeterColors.Player1 : MeterColors.Player2);
        DrawTiming(painter, side.PointName, side.PointMove, Padding + ChipWidth, y, HeaderHeight);
        DrawAdvantage(painter, side.Advantage, y);
    }

    private static void DrawTiming(Painter painter, string name, MoveTiming? move, float x, float y, float height)
    {
        x += painter.Write(name, x, y, height, MeterColors.Name) + ItemGap;

        if (move is null)
            return;

        x += painter.Write(MoveNames.Of(move.State), x, y, height, MeterColors.Move) + ItemGap;
        int[] values = [move.Startup, move.Active, move.Recovery, move.Total];

        for (var index = move.Startup == 0 ? TimingLabels.Length - 1 : 0; index < TimingLabels.Length; index++)
        {
            x += painter.Write(TimingLabels[index], x, y, height, MeterColors.Label) + LabelGap;
            x += painter.Write(values[index].ToString(), x, y, height, MeterColors.Value) + ItemGap;
        }
    }

    private static void DrawAdvantage(Painter painter, int? advantage, float y)
    {
        if (advantage is not { } value)
            return;

        var text = value > 0 ? $"+{value}" : value.ToString();
        var color = value > 0 ? MeterColors.Plus : value < 0 ? MeterColors.Minus : MeterColors.Value;
        var right = PanelWidth - Padding - painter.Width(text, HeaderHeight);
        painter.Write(text, right, y, HeaderHeight, color);
        painter.Write(AdvantageLabel, right - LabelGap - painter.Width(AdvantageLabel, HeaderHeight), y, HeaderHeight, MeterColors.Label);
    }

    private static void DrawLane(Painter painter, IReadOnlyList<MeterCell> cells, float y, float height)
    {
        var x = Padding + ChipWidth;
        painter.Fill(x, y, TrackWidth, height, MeterColors.Lane);

        for (var index = 0; index < cells.Count; index++)
            DrawCell(painter, cells[index], x + index * Column, y, height);
    }

    private static void DrawCell(Painter painter, MeterCell cell, float x, float y, float height)
    {
        if (cell.Kind == FrameKind.Empty)
            return;

        painter.Fill(x, y, CellWidth, height, MeterColors.Of(cell.Kind));

        if (cell.Marks.HasFlag(CellMarks.HitStart))
            painter.Fill(x, y, HitStartWidth, height, MeterColors.HitStart);

        if (cell.Marks.HasFlag(CellMarks.Projectile))
            painter.Fill(x + (CellWidth - MarkSize) / 2, y + height - MarkSize - 1, MarkSize, MarkSize, MeterColors.Projectile);
    }

    private static void DrawStrip(Painter painter, IReadOnlyList<MeterCell> cells, float y)
    {
        var x = Padding + ChipWidth;

        for (var index = 0; index < cells.Count; index++)
        {
            var cell = cells[index];

            if (cell.Invulnerability != Invulnerability.None)
                painter.Fill(x + index * Column, y, CellWidth, StripHeight, MeterColors.OfInvulnerability(cell.Invulnerability));
            else if (cell.Marks.HasFlag(CellMarks.Armor))
                painter.Fill(x + index * Column, y, CellWidth, StripHeight, MeterColors.Armor);
        }
    }

    private static void DrawCounts(Painter painter, IReadOnlyList<MeterCell> cells, float y, bool counts)
    {
        if (!counts)
            return;

        var x = Padding + ChipWidth;
        var start = 0;

        for (var index = 1; index <= cells.Count; index++)
        {
            if (index < cells.Count && cells[index].Kind == cells[start].Kind)
                continue;

            WriteRun(painter, cells[start].Kind, index - start, x + start * Column, y);
            start = index;
        }
    }

    private static void WriteRun(Painter painter, FrameKind kind, int length, float x, float y)
    {
        if (length < ShortestCountedRun || kind is FrameKind.Empty or FrameKind.Idle)
            return;

        var text = length.ToString();
        var width = painter.Width(text, CountHeight);
        var room = length * Column - 1;

        if (width > room)
            return;

        painter.Write(text, x + (room - width) / 2, y + (PointHeight - CountHeight) / 2, CountHeight, MeterColors.Count);
    }

    private static void DrawCenter(Painter painter, MeterView view, float y)
    {
        var left = Padding + ChipWidth;
        left += painter.Write(ComboText(view.Player2.Combo), left, y, CenterHeight, MeterColors.Player1) + ItemGap;
        WriteGap(painter, view.Player2.Gap, left, y);

        var rightText = ComboText(view.Player1.Combo);
        var right = PanelWidth - Padding - painter.Width(rightText, CenterHeight);
        painter.Write(rightText, right, y, CenterHeight, MeterColors.Player2);
        WriteGap(painter, view.Player1.Gap, right - ItemGap - painter.Width($"{GapLabel} {view.Player1.Gap}", CenterHeight), y);

        if (view.Freeze <= 0)
            return;

        var freeze = $"{FreezeLabel} {view.Freeze}";
        painter.Write(freeze, (PanelWidth - painter.Width(freeze, CenterHeight)) / 2, y, CenterHeight, MeterColors.Label);
    }

    private static void WriteGap(Painter painter, int? gap, float x, float y)
    {
        if (gap is { } frames)
            painter.Write($"{GapLabel} {frames}", x, y, CenterHeight, MeterColors.Label);
    }

    private static string ComboText(ComboReadout combo)
    {
        var parts = new List<string>();

        if (combo.Hits > 0)
            parts.Add($"{combo.Hits} {(combo.Hits == 1 ? "HIT" : "HITS")} {combo.Damage} DAMAGE");

        if (combo.Blocks > 0)
            parts.Add($"{combo.Blocks} BLOCKED {combo.Chip} CHIP");

        return string.Join("  ", parts);
    }

    private static IReadOnlyList<MeterCell> Window(IReadOnlyList<MeterCell> cells) =>
        cells.Count <= VisibleFrames ? cells : cells.Skip(cells.Count - VisibleFrames).ToArray();

    private enum RowKind
    {
        Space,
        Header,
        PartnerLine,
        Strip,
        PointLane,
        PartnerLane,
        Center,
    }

    private sealed record Row(RowKind Kind, Side Side, float Height);

    private sealed class Painter(HudScene scene, IHudFont font, float opacity, float size, float left, float top)
    {
        public void Fill(float x, float y, float width, float height, uint color) =>
            scene.Fill(left + x * size, top + y * size, width * size, height * size, MeterColors.Fade(color, opacity));

        public float Write(string text, float x, float y, float height, uint color)
        {
            if (text.Length == 0)
                return 0;

            var scale = height * size / font.CellHeight;
            scene.Write(text, left + x * size, top + y * size, scale, MeterColors.Fade(color, opacity));
            return Width(text, height);
        }

        public float Width(string text, float height) => font.Width(text, height * size / font.CellHeight) / size;
    }
}
