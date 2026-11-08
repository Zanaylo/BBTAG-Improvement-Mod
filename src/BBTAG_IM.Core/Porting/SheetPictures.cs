using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Porting;

public static class SheetPictures
{
    private const int DoubleScale = 2;

    public static string Name(string image, string prefix) => prefix + Path.GetFileNameWithoutExtension(image);

    public static byte[] Single(CgImage image, ReadOnlySpan<byte> palette)
    {
        var picture = new IndexedPicture(image.Width, image.Height, image.Left, image.Top, image.Indices);
        return HipWriter.Write(picture, HipWriter.PictureSide(image.CanvasWidth), HipWriter.PictureSide(image.CanvasHeight), palette);
    }

    public static byte[] Double(CgImage image, ReadOnlySpan<byte> palette)
    {
        var width = image.Width * DoubleScale;
        var height = image.Height * DoubleScale;
        var indices = new byte[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                indices[y * width + x] = image.Indices[y / DoubleScale * image.Width + x / DoubleScale];
        }

        var picture = new IndexedPicture(width, height, image.Left * DoubleScale, image.Top * DoubleScale, indices);
        return HipWriter.Write(picture, HipWriter.PictureSide(image.CanvasWidth) * DoubleScale, HipWriter.PictureSide(image.CanvasHeight) * DoubleScale, palette);
    }
}
