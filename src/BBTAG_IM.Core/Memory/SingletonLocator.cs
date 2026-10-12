using System.Buffers.Binary;
using Iced.Intel;

namespace BBTAG_IM.Core.Memory;

public sealed record SingletonPattern(string Name, BytePattern Site, int CallOffset);

public sealed class SingletonLocator(CodeImage image)
{
    private const int Bitness = 32;
    private const byte CallOpcode = 0xE8;
    private const int CallLength = 5;
    private const int MaximumGetterLength = 160;

    public uint Locate(SingletonPattern pattern) => AddressLoadedBy(LocateGetter(pattern), pattern);

    public uint LocateGetter(SingletonPattern pattern)
    {
        var call = image.FindSingle(pattern.Site, pattern.Name) + (uint)pattern.CallOffset;
        var callBytes = image.Slice(call, CallLength);

        if (callBytes.Length < CallLength || callBytes[0] != CallOpcode)
            throw new GameLayoutException($"{pattern.Name} does not call its getter. {GameLayoutException.UnsupportedVersion}");

        var getter = (uint)(call + CallLength + BinaryPrimitives.ReadInt32LittleEndian(callBytes[1..]));
        AddressLoadedBy(getter, pattern);
        return getter;
    }

    private uint AddressLoadedBy(uint getter, SingletonPattern pattern) =>
        FirstAddressLoadedBy(getter)
            ?? throw new GameLayoutException($"The {pattern.Name} getter holds no object address. {GameLayoutException.UnsupportedVersion}");

    private uint? FirstAddressLoadedBy(uint getter)
    {
        var reader = new ByteArrayCodeReader(image.Slice(getter, MaximumGetterLength).ToArray());
        var decoder = Decoder.Create(Bitness, reader, getter);

        while (reader.CanReadByte)
        {
            decoder.Decode(out var instruction);

            if (instruction.IsInvalid || instruction.FlowControl == FlowControl.Return)
                return null;

            if (IsAddressLoad(instruction))
                return instruction.Immediate32;
        }

        return null;
    }

    private static bool IsAddressLoad(Instruction instruction) =>
        instruction.Mnemonic == Mnemonic.Mov
        && instruction.Op0Kind == OpKind.Register
        && instruction.Op1Kind == OpKind.Immediate32;
}
