using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed class ByteWriter(int capacity)
{
    private readonly byte[] _buffer = new byte[capacity];

    public int Length { get; private set; }

    public void UInt16At(int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(offset), value);
        Grow(offset + sizeof(ushort));
    }

    public void UInt32At(int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(offset), value);
        Grow(offset + sizeof(uint));
    }

    public void BytesAt(int offset, ReadOnlySpan<byte> value)
    {
        value.CopyTo(_buffer.AsSpan(offset));
        Grow(offset + value.Length);
    }

    public int AsciiAt(int offset, string value)
    {
        BytesAt(offset, Encoding.ASCII.GetBytes(value + "\0"));
        return offset + value.Length + 1;
    }

    public byte[] ToArray(int? length = null) => _buffer.AsSpan(0, length ?? Length).ToArray();

    private void Grow(int end) => Length = Math.Max(Length, end);
}
