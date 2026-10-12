namespace BBTAG_IM.Core.Palettes;

public readonly record struct PaletteColor(byte R, byte G, byte B, byte A);

public sealed class PaletteColors
{
    public const int FileCount = ImplPalette.FileCount;
    public const int FileLength = ImplPalette.FileLength;
    public const int ColorsPerFile = ImplPalette.ColorCount;
    public const int BytesPerColor = sizeof(uint);
    public const int Length = FileCount * FileLength;

    private const int BlueByte = 0;
    private const int GreenByte = 1;
    private const int RedByte = 2;
    private const int AlphaByte = 3;
    private const byte Full = 0xFF;
    private const int RedWeight = 30;
    private const int GreenWeight = 59;
    private const int BlueWeight = 11;
    private const int WeightTotal = 100;
    private const int DimDivisor = 2;

    private static readonly PaletteColor LitFlash = new(Full, 0, Full, Full);
    private static readonly PaletteColor UnlitFlash = new(0, Full, Full, Full);

    private readonly byte[] _bytes;

    public PaletteColors()
        : this(new byte[Length])
    {
    }

    private PaletteColors(byte[] bytes) => _bytes = bytes;

    public ReadOnlySpan<byte> Bytes => _bytes;

    public static PaletteColors From(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Length)
            throw new ArgumentException($"Palette colors are {Length} bytes, not {bytes.Length}.", nameof(bytes));

        return new PaletteColors(bytes.ToArray());
    }

    public ReadOnlySpan<byte> File(int file) => _bytes.AsSpan(file * FileLength, FileLength);

    public PaletteColor Get(int file, int entry) => Decode(Color(file, entry));

    public static PaletteColor Decode(ReadOnlySpan<byte> color) => new(color[RedByte], color[GreenByte], color[BlueByte], color[AlphaByte]);

    public void Set(int file, int entry, PaletteColor value)
    {
        var color = Color(file, entry);
        color[RedByte] = value.R;
        color[GreenByte] = value.G;
        color[BlueByte] = value.B;
        color[AlphaByte] = value.A;
    }

    public PaletteColors Copy() => new(_bytes.ToArray());

    public bool SameAs(PaletteColors other) => _bytes.AsSpan().SequenceEqual(other._bytes);

    public PaletteColors Flashing(int file, int entry, bool lit)
    {
        var flashing = Dimmed();
        var flash = lit ? LitFlash : UnlitFlash;
        flashing.Set(file, entry, flash with { A = Get(file, entry).A });
        return flashing;
    }

    public PaletteColors Wearing(ImplPalette palette)
    {
        var worn = Copy();

        for (var file = 0; file < FileCount; file++)
        {
            if (!palette.IsFileEmpty(file))
                palette.File(file).CopyTo(worn._bytes.AsSpan(file * FileLength));
        }

        return worn;
    }

    private PaletteColors Dimmed()
    {
        var dimmed = Copy();

        for (var at = 0; at < Length; at += BytesPerColor)
        {
            var color = dimmed._bytes.AsSpan(at, BytesPerColor);
            var grey = (byte)((color[RedByte] * RedWeight + color[GreenByte] * GreenWeight + color[BlueByte] * BlueWeight) / WeightTotal / DimDivisor);
            color[RedByte] = grey;
            color[GreenByte] = grey;
            color[BlueByte] = grey;
        }

        return dimmed;
    }

    private Span<byte> Color(int file, int entry) => _bytes.AsSpan(file * FileLength + entry * BytesPerColor, BytesPerColor);
}
