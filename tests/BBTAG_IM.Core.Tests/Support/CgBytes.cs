using System.Text;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed class CgBytes
{
    private const int SignatureLength = 16;
    private const int EmbeddedPaletteLength = 0x2000;
    private const int FieldsBeforeIndex = 12;
    private const int FieldsWritten = 4;
    private const int IndexEntries = 3001;
    private const int AlignmentEntry = 3000;
    private const int NameLength = 32;
    private const int TileSide = 16;

    private readonly List<CgImageEntry> _images = [];

    public CgBytes Image(string name, int canvas, int left, int top, int right, int bottom, byte[][] tiles, (int X, int Y)[] places, int bpp = 8)
    {
        _images.Add(new CgImageEntry(name, canvas, left, top, right, bottom, tiles, places, bpp));
        return this;
    }

    public byte[] Build()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("BMP Cutter3".PadRight(SignatureLength, '\0')));
        writer.Write(0);
        writer.Write(new byte[EmbeddedPaletteLength]);
        var index = new int[IndexEntries];
        writer.Write(Math.Max(_images.Count - 1, 0));
        writer.Write(0);
        writer.Write(_images.Sum(image => image.Places.Length));
        writer.Write(_images.Count);

        for (var field = FieldsWritten; field < FieldsBeforeIndex; field++)
            writer.Write(0);

        var indexStart = (int)stream.Position;
        writer.Write(new byte[IndexEntries * sizeof(int)]);
        var firstAlignment = 0;
        List<CgAlignmentEntry> alignments = [];

        for (var image = 0; image < _images.Count; image++)
        {
            index[image] = (int)stream.Position;
            WriteImage(writer, _images[image], firstAlignment, image, alignments);
            firstAlignment += _images[image].Places.Length;
        }

        index[AlignmentEntry] = (int)stream.Position;

        foreach (var alignment in alignments)
            WriteAlignment(writer, alignment);

        stream.Position = indexStart;

        foreach (var value in index)
            writer.Write(value);

        writer.Flush();
        return stream.ToArray();
    }

    private static void WriteImage(BinaryWriter writer, CgImageEntry entry, int firstAlignment, int page, List<CgAlignmentEntry> alignments)
    {
        var name = new byte[NameLength];
        Encoding.ASCII.GetBytes(entry.Name, name);
        writer.Write(name);
        writer.Write(0);
        writer.Write(entry.Canvas);
        writer.Write(entry.Canvas);
        writer.Write(entry.Bpp);
        writer.Write(entry.Left);
        writer.Write(entry.Top);
        writer.Write(entry.Right);
        writer.Write(entry.Bottom);
        writer.Write(firstAlignment);
        writer.Write(entry.Places.Length);

        for (var tile = 0; tile < entry.Places.Length; tile++)
        {
            writer.Write(entry.Tiles[tile]);
            alignments.Add(new CgAlignmentEntry(entry.Places[tile].X, entry.Places[tile].Y, (short)(tile * TileSide), (short)page));
        }
    }

    private static void WriteAlignment(BinaryWriter writer, CgAlignmentEntry alignment)
    {
        writer.Write(alignment.X);
        writer.Write(alignment.Y);
        writer.Write(TileSide);
        writer.Write(TileSide);
        writer.Write(alignment.SourceX);
        writer.Write((short)0);
        writer.Write(alignment.Page);
        writer.Write((short)0);
    }

    private sealed record CgImageEntry(string Name, int Canvas, int Left, int Top, int Right, int Bottom, byte[][] Tiles, (int X, int Y)[] Places, int Bpp);

    private sealed record CgAlignmentEntry(int X, int Y, short SourceX, short Page);
}
