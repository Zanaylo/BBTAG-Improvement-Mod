using System.Buffers.Binary;
using System.Numerics;
using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Sprites;

public sealed record IndexedPicture(int Width, int Height, int X, int Y, byte[] Indices);

public static class HipWriter
{
    private const int FormatField = 4;
    private const int LengthField = 8;
    private const int PaletteCountField = 0x0C;
    private const int PictureWidthField = 0x10;
    private const int PictureHeightField = 0x14;
    private const int FlagsField = 0x18;
    private const int ImageHeaderLengthField = 0x1C;
    private const int WidthField = 0x20;
    private const int HeightField = 0x24;
    private const int XField = 0x28;
    private const int YField = 0x2C;
    private const int ImageHeaderLength = 0x20;
    private const int PaletteStart = WidthField + ImageHeaderLength;
    private const int PaletteLength = HplWriter.ColorCount * sizeof(int);
    private const int PixelsStart = PaletteStart + PaletteLength;
    private const int FormatVersion = 0x125;
    private const int IndexedRunFlags = 0x20010101;
    private const int LongestRun = byte.MaxValue;
    private const int BytesPerRun = 2;

    private static readonly byte[] Signature = "HIP\0"u8.ToArray();

    public static int PictureSide(int canvasSide) => (int)BitOperations.RoundUpToPowerOf2((uint)canvasSide);

    public static byte[] Write(IndexedPicture picture, int pictureWidth, int pictureHeight, ReadOnlySpan<byte> rgbaPalette)
    {
        var crop = Crop(picture);
        var runs = Runs(picture, crop);
        var hip = new byte[PixelsStart + runs.Count * BytesPerRun];
        var span = hip.AsSpan();
        Signature.CopyTo(span);
        Write(span, FormatField, FormatVersion);
        Write(span, LengthField, hip.Length);
        Write(span, PaletteCountField, HplWriter.ColorCount);
        Write(span, PictureWidthField, pictureWidth);
        Write(span, PictureHeightField, pictureHeight);
        Write(span, FlagsField, IndexedRunFlags);
        Write(span, ImageHeaderLengthField, ImageHeaderLength);
        Write(span, WidthField, crop.Width);
        Write(span, HeightField, crop.Height);
        Write(span, XField, picture.X + crop.X);
        Write(span, YField, picture.Y + crop.Y);
        HplWriter.CopyBlueFirst(rgbaPalette, span[PaletteStart..]);
        var cursor = PixelsStart;

        foreach (var (index, length) in runs)
        {
            hip[cursor++] = index;
            hip[cursor++] = length;
        }

        return hip;
    }

    private static void Write(Span<byte> span, int offset, int value) => BinaryPrimitives.WriteInt32LittleEndian(span[offset..], value);

    private static CropArea Crop(IndexedPicture picture)
    {
        var left = picture.Width;
        var right = -1;
        var top = picture.Height;
        var bottom = -1;

        for (var y = 0; y < picture.Height; y++)
        {
            for (var x = 0; x < picture.Width; x++)
            {
                if (picture.Indices[y * picture.Width + x] == 0)
                    continue;

                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
            }
        }

        return right < 0 ? new CropArea(0, 0, 1, 1) : new CropArea(left, top, right - left + 1, bottom - top + 1);
    }

    private static List<(byte Index, byte Length)> Runs(IndexedPicture picture, CropArea crop)
    {
        List<(byte Index, byte Length)> runs = [];
        byte current = 0;
        var length = 0;

        for (var y = crop.Y; y < crop.Y + crop.Height; y++)
        {
            for (var x = crop.X; x < crop.X + crop.Width; x++)
            {
                var index = picture.Indices[y * picture.Width + x];

                if (length > 0 && (index != current || length == LongestRun))
                {
                    runs.Add((current, (byte)length));
                    length = 0;
                }

                current = index;
                length++;
            }
        }

        runs.Add((current, (byte)length));
        return runs;
    }

    private readonly record struct CropArea(int X, int Y, int Width, int Height);
}
