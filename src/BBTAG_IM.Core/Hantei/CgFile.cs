using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Hantei;

public sealed record CgImage(string Name, int CanvasWidth, int CanvasHeight, int Left, int Top, int Width, int Height, byte[] Indices);

public sealed class CgFile
{
    private const int HeaderStart = 0x14 + 0x2000;
    private const int PageCountField = 0;
    private const int AlignmentCountField = 2 * sizeof(int);
    private const int ImageCountField = 3 * sizeof(int);
    private const int IndexField = 12 * sizeof(int);
    private const int AlignmentIndexEntry = 3000;
    private const int LargestImageCount = 3000;
    private const int NameLength = 32;
    private const int TypeField = NameLength;
    private const int CanvasWidthField = TypeField + sizeof(int);
    private const int CanvasHeightField = CanvasWidthField + sizeof(int);
    private const int BppField = CanvasHeightField + sizeof(int);
    private const int LeftField = BppField + sizeof(int);
    private const int TopField = LeftField + sizeof(int);
    private const int RightField = TopField + sizeof(int);
    private const int BottomField = RightField + sizeof(int);
    private const int AlignmentStartField = BottomField + sizeof(int);
    private const int AlignmentLengthField = AlignmentStartField + sizeof(int);
    private const int ImageDataField = AlignmentLengthField + sizeof(int);
    private const int AlignmentLength = 24;
    private const int AlignmentHeightField = 3 * sizeof(int);
    private const int AlignmentSourceXField = 4 * sizeof(int);
    private const int AlignmentSourceYField = AlignmentSourceXField + sizeof(short);
    private const int AlignmentPageField = AlignmentSourceYField + sizeof(short);
    private const int AlignmentCopyField = AlignmentPageField + sizeof(short);
    private const int CellSide = 0x10;
    private const int CellsPerPage = CellSide * CellSide;
    private const int IgnoredType = -1;
    private const int IndexedDepth = 8;
    private const int ColorKeyedType = 3;
    private const int IndexedAlphaType = 4;
    private const int PalettedType = 2;
    private const int TrueColorDepth = 32;
    private const int ColorKeyLength = 4;
    private const int EmbeddedPaletteLength = 1024;

    private static readonly byte[] Signature = "BMP Cutter3"u8.ToArray();

    private readonly byte[] _bytes;
    private readonly int[] _index;
    private readonly int _alignmentStart;
    private readonly int _alignmentCount;
    private readonly Cell[][] _pages;

    private CgFile(byte[] bytes, int[] index, int alignmentStart, int alignmentCount, int pageCount)
    {
        _bytes = bytes;
        _index = index;
        _alignmentStart = alignmentStart;
        _alignmentCount = alignmentCount;
        _pages = new Cell[pageCount][];

        for (var page = 0; page < pageCount; page++)
            _pages[page] = new Cell[CellsPerPage];

        BuildCells();
    }

    public int ImageCount => _index.Length;

    public static CgFile Read(byte[] bytes)
    {
        if (bytes.Length < HeaderStart + IndexField || !bytes.AsSpan().StartsWith(Signature))
            throw new InvalidHanteiDataException("It is not a sprite sheet.");

        var header = bytes.AsSpan(HeaderStart);
        var pageCount = Int32(header, PageCountField) + 1;
        var alignmentCount = Int32(header, AlignmentCountField);
        var imageCount = Int32(header, ImageCountField);

        if (imageCount is < 0 or > LargestImageCount)
            throw new InvalidHanteiDataException("The sprite sheet lists too many images.");

        var index = new int[imageCount];

        for (var image = 0; image < imageCount; image++)
            index[image] = Int32(header, IndexField + image * sizeof(int));

        return new CgFile(bytes, index, Int32(header, IndexField + AlignmentIndexEntry * sizeof(int)), alignmentCount, pageCount);
    }

    public CgImage? Image(int number)
    {
        if (number < 0 || number >= _index.Length || _index[number] <= 0)
            return null;

        var header = _bytes.AsSpan(_index[number]);

        if (Int32(header, TypeField) == IgnoredType || Int32(header, BppField) > IndexedDepth)
            return null;

        var left = Int32(header, LeftField);
        var top = Int32(header, TopField);
        var width = Int32(header, RightField) - left + 1;
        var height = Int32(header, BottomField) - top + 1;

        if (width <= 0 || height <= 0)
            return null;

        var indices = new byte[width * height];
        var start = Int32(header, AlignmentStartField);

        for (var alignment = 0; alignment < Int32(header, AlignmentLengthField); alignment++)
            CopyCells(Alignment(start + alignment), indices, left, top, width, height);

        return new CgImage(ReadName(header), Int32(header, CanvasWidthField), Int32(header, CanvasHeightField), left, top, width, height, indices);
    }

    private static int Int32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]);

    private static short Int16(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadInt16LittleEndian(bytes[offset..]);

    private static string ReadName(ReadOnlySpan<byte> header)
    {
        var field = header[..NameLength];
        var terminator = field.IndexOf((byte)0);
        return Encoding.ASCII.GetString(terminator < 0 ? field : field[..terminator]);
    }

    private Alignment Alignment(int index)
    {
        var entry = _bytes.AsSpan(_alignmentStart + index * AlignmentLength, AlignmentLength);

        return new Alignment(
            Int32(entry, 0),
            Int32(entry, sizeof(int)),
            Int32(entry, 2 * sizeof(int)),
            Int32(entry, AlignmentHeightField),
            Int16(entry, AlignmentSourceXField),
            Int16(entry, AlignmentSourceYField),
            Int16(entry, AlignmentPageField),
            Int16(entry, AlignmentCopyField));
    }

    private void BuildCells()
    {
        foreach (var start in _index.Where(start => start > 0))
            AddImageCells(start);
    }

    private void AddImageCells(int start)
    {
        var header = _bytes.AsSpan(start);
        var type = Int32(header, TypeField);
        var alignmentStart = Int32(header, AlignmentStartField);
        var alignmentLength = Int32(header, AlignmentLengthField);

        if (type == IgnoredType || alignmentStart + alignmentLength > _alignmentCount)
            return;

        var address = start + ImageDataField + PaletteLength(type, Int32(header, BppField));
        var scale = Int32(header, BppField) == TrueColorDepth && type is not (PalettedType or IndexedAlphaType or ColorKeyedType) ? sizeof(int) : 1;

        for (var index = 0; index < alignmentLength; index++)
        {
            var alignment = Alignment(alignmentStart + index);

            if (alignment.CopyFlag != 0)
                continue;

            AddCells(alignment, address);
            address += alignment.Width * alignment.Height * (type == IndexedAlphaType ? 2 : scale);
        }
    }

    private static int PaletteLength(int type, int bpp)
    {
        if (bpp != TrueColorDepth)
            return 0;

        return type switch
        {
            ColorKeyedType => ColorKeyLength,
            PalettedType or IndexedAlphaType => EmbeddedPaletteLength,
            _ => 0,
        };
    }

    private void AddCells(Alignment alignment, int address)
    {
        var column = alignment.SourceX / CellSide;
        var row = alignment.SourceY / CellSide;
        var columns = Math.Min(alignment.Width / CellSide, CellSide - column);
        var rows = Math.Min(alignment.Height / CellSide, CellSide - row);
        var page = _pages[alignment.Page];

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
                page[(row + y) * CellSide + column + x] = new Cell(address, alignment.Width, x * CellSide + y * alignment.Width * CellSide);
        }
    }

    private void CopyCells(Alignment alignment, byte[] indices, int left, int top, int width, int height)
    {
        var page = _pages[alignment.Page];
        var firstCell = alignment.SourceY / CellSide * CellSide + alignment.SourceX / CellSide;

        for (var y = 0; y < alignment.Height / CellSide; y++)
        {
            for (var x = 0; x < alignment.Width / CellSide; x++)
            {
                var number = firstCell + y * CellSide + x;

                if (number < CellsPerPage && page[number].Start != 0)
                    CopyCell(page[number], alignment.X + x * CellSide - left, alignment.Y + y * CellSide - top, indices, width, height);
            }
        }
    }

    private void CopyCell(Cell cell, int x, int y, byte[] indices, int width, int height)
    {
        var source = cell.Start + cell.Offset;

        for (var row = 0; row < CellSide; row++)
        {
            var targetY = y + row;

            if (targetY < 0 || targetY >= height)
                continue;

            for (var column = 0; column < CellSide; column++)
            {
                var targetX = x + column;

                if (targetX >= 0 && targetX < width)
                    indices[targetY * width + targetX] = _bytes[source + row * cell.Stride + column];
            }
        }
    }

    private readonly record struct Cell(int Start, int Stride, int Offset);
}

internal readonly record struct Alignment(int X, int Y, int Width, int Height, short SourceX, short SourceY, short Page, short CopyFlag);
