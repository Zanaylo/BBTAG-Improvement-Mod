using System.Buffers.Binary;
using System.Globalization;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed class GameCode
{
    public const byte Breakpoint = 0xCC;
    public const byte Return = 0xC3;
    public const byte MoveImmediateToEcx = 0xB9;
    public const int GetterAddressOffset = 26;

    private const byte Call = 0xE8;
    private const int CallLength = 5;

    private static readonly byte[] GetterTemplate =
    [
        0x64, 0xA1, 0x2C, 0x00, 0x00, 0x00,
        0x8B, 0x0D, 0x98, 0xA2, 0x16, 0x02,
        0x8B, 0x0C, 0x88,
        0xA1, 0x38, 0xDA, 0xE5, 0x00,
        0x3B, 0x41, 0x04,
        0x7F, 0x06,
        0xB8, 0x00, 0x00, 0x00, 0x00,
        Return,
    ];

    private readonly byte[] _bytes;

    public GameCode(int size)
    {
        _bytes = new byte[size];
        Array.Fill(_bytes, Breakpoint);
    }

    public static int GetterLength => GetterTemplate.Length;

    public GameCode Bytes(uint offset, string hex)
    {
        var bytes = hex.Split(' ').Select(token => token == "??" ? Breakpoint : byte.Parse(token, NumberStyles.HexNumber)).ToArray();
        bytes.CopyTo(_bytes, offset);
        return this;
    }

    public GameCode CallAt(uint offset, uint targetOffset)
    {
        _bytes[offset] = Call;
        BinaryPrimitives.WriteInt32LittleEndian(_bytes.AsSpan((int)offset + 1), (int)(targetOffset - offset - CallLength));
        return this;
    }

    public GameCode SingletonGetterAt(uint offset, uint objectAddress)
    {
        GetterTemplate.CopyTo(_bytes, offset);
        BinaryPrimitives.WriteUInt32LittleEndian(_bytes.AsSpan((int)offset + GetterAddressOffset), objectAddress);
        return this;
    }

    public byte[] ToArray() => _bytes.ToArray();
}
