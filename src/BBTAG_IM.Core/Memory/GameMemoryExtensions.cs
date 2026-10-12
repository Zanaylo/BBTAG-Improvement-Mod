using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;

namespace BBTAG_IM.Core.Memory;

public static class GameMemoryExtensions
{
    private const int MatrixSize = 16 * sizeof(float);

    public static uint ReadUInt32(this IGameMemory memory, uint address)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        memory.Read(address, bytes);
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    public static int ReadInt32(this IGameMemory memory, uint address) => (int)memory.ReadUInt32(address);

    public static float ReadSingle(this IGameMemory memory, uint address) => BitConverter.UInt32BitsToSingle(memory.ReadUInt32(address));

    public static uint? ReadPointer(this IGameMemory memory, uint address)
    {
        if (!memory.CanRead(address, sizeof(uint)))
            return null;

        var pointer = memory.ReadUInt32(address);
        return pointer == 0 ? null : pointer;
    }

    public static Matrix4x4 ReadMatrix(this IGameMemory memory, uint address)
    {
        Span<byte> bytes = stackalloc byte[MatrixSize];
        memory.Read(address, bytes);
        return MemoryMarshal.Read<Matrix4x4>(bytes);
    }

    public static byte[] ReadBytes(this IGameMemory memory, uint address, int length)
    {
        var bytes = new byte[length];
        memory.Read(address, bytes);
        return bytes;
    }

    public static void WriteInt32(this IGameMemory memory, uint address, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        memory.Write(address, bytes);
    }
}
