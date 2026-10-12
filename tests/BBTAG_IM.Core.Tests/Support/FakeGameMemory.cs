using System.Buffers.Binary;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed class FakeGameMemory : IGameMemory
{
    private readonly List<MappedRegion> _regions = [];

    public FakeGameMemory Map(uint address, int length, bool writable = true)
    {
        _regions.Add(new MappedRegion(address, new byte[length], writable));
        return this;
    }

    public FakeGameMemory UInt32At(uint address, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(Bytes(address, sizeof(uint)), value);
        return this;
    }

    public FakeGameMemory Int32At(uint address, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(Bytes(address, sizeof(int)), value);
        return this;
    }

    public FakeGameMemory SingleAt(uint address, float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(Bytes(address, sizeof(float)), value);
        return this;
    }

    public FakeGameMemory BytesAt(uint address, ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(Bytes(address, bytes.Length));
        return this;
    }

    public byte[] BytesFrom(uint address, int length) => Bytes(address, length).ToArray();

    public int Int32From(uint address) => BinaryPrimitives.ReadInt32LittleEndian(Bytes(address, sizeof(int)));

    public bool CanRead(uint address, int length) => FindRegion(address, length) is not null;

    public bool CanWrite(uint address, int length) => FindRegion(address, length) is { Writable: true };

    public void Read(uint address, Span<byte> destination) => Bytes(address, destination.Length).CopyTo(destination);

    public void Write(uint address, ReadOnlySpan<byte> source)
    {
        if (!CanWrite(address, source.Length))
            throw new InvalidOperationException($"The game would crash writing {source.Length} bytes at 0x{address:X8}.");

        source.CopyTo(Bytes(address, source.Length));
    }

    private Span<byte> Bytes(uint address, int length)
    {
        var region = FindRegion(address, length)
            ?? throw new InvalidOperationException($"The game would crash reading {length} bytes at 0x{address:X8}.");

        return region.Bytes.AsSpan((int)(address - region.Address), length);
    }

    private MappedRegion? FindRegion(uint address, int length) => _regions.FirstOrDefault(region => region.Covers(address, length));

    private sealed record MappedRegion(uint Address, byte[] Bytes, bool Writable)
    {
        public bool Covers(uint address, int length) =>
            address >= Address && address + (ulong)length <= Address + (ulong)Bytes.Length;
    }
}
