namespace BBTAG_IM.Core.Meter;

public readonly record struct HudRect(float X, float Y, float Width, float Height, uint Color);

public readonly record struct HudText(string Text, float X, float Y, float Scale, uint Color);

public sealed class HudScene
{
    private readonly List<HudRect> _rects = [];
    private readonly List<HudText> _texts = [];

    public IReadOnlyList<HudRect> Rects => _rects;

    public IReadOnlyList<HudText> Texts => _texts;

    public void Fill(float x, float y, float width, float height, uint color)
    {
        if (width <= 0 || height <= 0 || color >> 24 == 0)
            return;

        _rects.Add(new HudRect(x, y, width, height, color));
    }

    public void Write(string text, float x, float y, float scale, uint color)
    {
        if (text.Length == 0 || color >> 24 == 0)
            return;

        _texts.Add(new HudText(text, x, y, scale, color));
    }

    public void Clear()
    {
        _rects.Clear();
        _texts.Clear();
    }
}
