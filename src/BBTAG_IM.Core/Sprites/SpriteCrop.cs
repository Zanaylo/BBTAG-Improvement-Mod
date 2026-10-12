namespace BBTAG_IM.Core.Sprites;

public static class SpriteCrop
{
    private const int Clear = 0;
    private const int Unlabelled = 0;
    private const int SmallestShareOfLargestPart = 20;

    public static IndexedSprite MainBody(IndexedSprite sprite)
    {
        var (labels, sizes) = Parts(sprite);

        if (sizes.Count == 0)
            return sprite;

        var smallest = sizes.Max() / SmallestShareOfLargestPart;
        var kept = sizes.Select(size => size > smallest).ToArray();

        return Cut(sprite, Bounds(sprite, labels, kept));
    }

    private static (int[] Labels, List<int> Sizes) Parts(IndexedSprite sprite)
    {
        var labels = new int[sprite.Indices.Length];
        var sizes = new List<int>();
        var pending = new Stack<int>();

        for (var pixel = 0; pixel < labels.Length; pixel++)
        {
            if (sprite.Indices[pixel] == Clear || labels[pixel] != Unlabelled)
                continue;

            sizes.Add(Fill(sprite, labels, pending, pixel, sizes.Count + 1));
        }

        return (labels, sizes);
    }

    private static int Fill(IndexedSprite sprite, int[] labels, Stack<int> pending, int start, int label)
    {
        var size = 0;
        labels[start] = label;
        pending.Push(start);

        while (pending.TryPop(out var pixel))
        {
            size++;
            var x = pixel % sprite.Width;
            var y = pixel / sprite.Width;

            Visit(sprite, labels, pending, x - 1, y, label);
            Visit(sprite, labels, pending, x + 1, y, label);
            Visit(sprite, labels, pending, x, y - 1, label);
            Visit(sprite, labels, pending, x, y + 1, label);
        }

        return size;
    }

    private static void Visit(IndexedSprite sprite, int[] labels, Stack<int> pending, int x, int y, int label)
    {
        if (x < 0 || y < 0 || x >= sprite.Width || y >= sprite.Height)
            return;

        var pixel = y * sprite.Width + x;

        if (sprite.Indices[pixel] == Clear || labels[pixel] != Unlabelled)
            return;

        labels[pixel] = label;
        pending.Push(pixel);
    }

    private static (int Left, int Top, int Right, int Bottom) Bounds(IndexedSprite sprite, int[] labels, bool[] kept)
    {
        var (left, top, right, bottom) = (sprite.Width, sprite.Height, -1, -1);

        for (var pixel = 0; pixel < labels.Length; pixel++)
        {
            if (labels[pixel] == Unlabelled || !kept[labels[pixel] - 1])
                continue;

            var x = pixel % sprite.Width;
            var y = pixel / sprite.Width;
            (left, top, right, bottom) = (Math.Min(left, x), Math.Min(top, y), Math.Max(right, x), Math.Max(bottom, y));
        }

        return (left, top, right, bottom);
    }

    private static IndexedSprite Cut(IndexedSprite sprite, (int Left, int Top, int Right, int Bottom) bounds)
    {
        var width = bounds.Right - bounds.Left + 1;
        var height = bounds.Bottom - bounds.Top + 1;
        var indices = new byte[width * height];

        for (var row = 0; row < height; row++)
            sprite.Indices.AsSpan((bounds.Top + row) * sprite.Width + bounds.Left, width).CopyTo(indices.AsSpan(row * width));

        return new IndexedSprite(width, height, indices);
    }
}
