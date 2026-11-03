using System.Numerics;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public sealed record BoxCanvas(float Scale, Vector2 Origin)
{
    private const float Fill = 0.9f;
    private const float SmallestSpan = 100;

    public BoxAxes Axes => new(-Scale, Scale);

    public static BoxCanvas Fit(IEnumerable<JonbinBox> boxes, Vector2 topLeft, Vector2 size)
    {
        var low = Vector2.Zero;
        var high = Vector2.Zero;

        foreach (var box in boxes)
        {
            low = Vector2.Min(low, new Vector2(-box.HighX, box.LowY));
            high = Vector2.Max(high, new Vector2(-box.LowX, box.HighY));
        }

        var span = Vector2.Max(high - low, new Vector2(SmallestSpan));
        var scale = MathF.Min(size.X / span.X, size.Y / span.Y) * Fill;
        var center = (low + high) / 2;

        return new BoxCanvas(scale, topLeft + size / 2 - center * scale);
    }

    public ScreenRect ToCanvas(JonbinBox box) =>
        new(
            Origin.X - box.HighX * Scale,
            Origin.Y + box.LowY * Scale,
            Origin.X - box.LowX * Scale,
            Origin.Y + box.HighY * Scale);
}
