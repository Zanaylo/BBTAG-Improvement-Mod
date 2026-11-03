namespace BBTAG_IM.Core.Sprites;

public static class SpriteTrim
{
    private const byte Clear = 0;

    public static PlacedSprite Trim(PlacedSprite sprite)
    {
        var image = sprite.Image;
        int left = image.Width, top = image.Height, right = -1, bottom = -1;

        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                if (image.Indices[y * image.Width + x] == Clear)
                    continue;

                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
            }
        }

        if (right < 0 || (left == 0 && top == 0 && right == image.Width - 1 && bottom == image.Height - 1))
            return sprite;

        var width = right - left + 1;
        var height = bottom - top + 1;
        var indices = new byte[width * height];

        for (var row = 0; row < height; row++)
            image.Indices.AsSpan((top + row) * image.Width + left, width).CopyTo(indices.AsSpan(row * width));

        return sprite with { Image = new IndexedSprite(width, height, indices), X = sprite.X + left, Y = sprite.Y + top };
    }
}
