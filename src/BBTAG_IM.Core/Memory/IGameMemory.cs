namespace BBTAG_IM.Core.Memory;

public interface IGameMemory
{
    bool CanRead(uint address, int length);

    bool CanWrite(uint address, int length);

    void Read(uint address, Span<byte> destination);

    void Write(uint address, ReadOnlySpan<byte> source);
}
