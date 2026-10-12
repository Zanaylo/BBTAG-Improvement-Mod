using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Pe;

public sealed class FileImage : IImageReader
{
    public FileImage(byte[] bytes)
    {
        Bytes = bytes;
        Headers = PeHeaders.Parse(bytes);
    }

    public byte[] Bytes { get; }

    public PeHeaders Headers { get; }

    public static FileImage Load(string path) => new(File.ReadAllBytes(path));

    public int? FileOffsetOf(uint rva)
    {
        if (rva < Headers.SizeOfHeaders)
            return (int)rva;

        var section = Headers.Sections.FirstOrDefault(candidate =>
            rva >= candidate.VirtualAddress && rva - candidate.VirtualAddress < candidate.RawDataSize);

        if (section is null)
            return null;

        var offset = section.RawDataPointer + (rva - section.VirtualAddress);
        return offset < Bytes.Length ? (int)offset : null;
    }

    public ushort ReadUInt16(uint rva) => BinaryPrimitives.ReadUInt16LittleEndian(Read(rva, sizeof(ushort)));

    public uint ReadUInt32(uint rva) => BinaryPrimitives.ReadUInt32LittleEndian(Read(rva, sizeof(uint)));

    public string ReadAsciiString(uint rva)
    {
        var start = RequireOffset(rva);
        var length = Bytes.AsSpan(start).IndexOf((byte)0);

        if (length < 0)
            throw new InvalidPeImageException($"Unterminated string at RVA 0x{rva:X}.");

        return Encoding.ASCII.GetString(Bytes, start, length);
    }

    public ReadOnlySpan<byte> SectionData(PeSection section)
    {
        var available = Math.Max(0, Bytes.Length - (int)section.RawDataPointer);
        return Bytes.AsSpan((int)section.RawDataPointer, Math.Min((int)section.RawDataSize, available));
    }

    private ReadOnlySpan<byte> Read(uint rva, int length)
    {
        var offset = RequireOffset(rva);

        if (offset + length > Bytes.Length)
            throw new InvalidPeImageException($"RVA 0x{rva:X} runs past the end of the file.");

        return Bytes.AsSpan(offset, length);
    }

    private int RequireOffset(uint rva) =>
        FileOffsetOf(rva) ?? throw new InvalidPeImageException($"RVA 0x{rva:X} is not backed by file data.");
}
