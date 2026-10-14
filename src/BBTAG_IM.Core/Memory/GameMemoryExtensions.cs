using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace BBTAG_IM.Core.Memory;

public static class GameMemoryExtensions
{
    private const int MatrixSize = 16 * sizeof(float);
    private const byte MoveToEax = 0xB8;
    private const int GetterLength = 5;

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

    public static string ReadText(this IGameMemory memory, uint address, int length)
    {
        Span<byte> bytes = stackalloc byte[length];
        memory.Read(address, bytes);
        var terminator = bytes.IndexOf((byte)0);

        return Encoding.Latin1.GetString(terminator < 0 ? bytes : bytes[..terminator]);
    }

    public static uint? AddressLoadedBy(this IGameMemory memory, uint getter)
    {
        if (!memory.CanRead(getter, GetterLength))
            return null;

        Span<byte> code = stackalloc byte[GetterLength];
        memory.Read(getter, code);

        return code[0] == MoveToEax ? BinaryPrimitives.ReadUInt32LittleEndian(code[1..]) : null;
    }
}
