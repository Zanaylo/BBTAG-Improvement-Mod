using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public static class OwnerlessTakemiGuardCode
{
    public const int SkipsOffset = 0;
    public const int CodeOffset = 8;
    public const int BlockLength = 64;
    public const int JumpLength = 5;
    public const int ReplacedLength = 6;

    private const string Name = "Take-Mikazuchi body collision";
    private const uint OwnerField = 0x1F4;
    private const byte JumpNear = 0xE9;
    private const byte NoOperation = 0x90;

    private static readonly BytePattern HandlerStart =
        BytePattern.Parse("55 8B EC 83 EC 18 53 8B D9 56 57 8B 83 A8 01 00 00 8B 73 30 89 45 F4 E8");

    private static readonly byte[] ReplacedInstructions = [0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x18];

    public static uint Locate(CodeImage image) => image.FindSingle(HandlerStart, Name);

    public static byte[] Code(uint site, uint block)
    {
        byte[] skipOwnerless =
        [
            0x83, 0xB9, .. Address(OwnerField), 0x00,
            0x75, 0x09,
            0xFF, 0x05, .. Address(block + SkipsOffset),
            0xC2, 0x04, 0x00,
            .. ReplacedInstructions,
        ];
        var jumpEnd = block + CodeOffset + (uint)skipOwnerless.Length + JumpLength;

        return [.. skipOwnerless, JumpNear, .. Address(site + ReplacedLength - jumpEnd)];
    }

    public static byte[] Jump(uint site, uint block) =>
        [JumpNear, .. Address(block + CodeOffset - (site + JumpLength)), NoOperation];

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
