using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public static class SittingOutSpawnGateCode
{
    public const int SkipsOffset = 0;
    public const int CodeOffset = 8;
    public const int BlockLength = 96;
    public const int JumpLength = 5;
    public const int ReplacedLength = 8;

    private const string Name = "Script object creation";
    private const byte SlotField = 0x30;
    private const byte LastSlot = MatchSlots.Count - 1;
    private const byte SlotStride = 0x28;
    private const uint FirstPresenceField = 0x27C4;
    private const byte SitsOut = (byte)SlotPresence.SitsOut;
    private const byte JumpNear = 0xE9;
    private const byte NoOperation = 0x90;

    private static readonly BytePattern HandlerStart =
        BytePattern.Parse("8B 45 08 B9 08 00 00 00 8B 75 08 8B 13 0F B7 40 20 50 83 EC 20 8B FC F3 A5 8B CB FF 92 58 05 00 00");

    private static readonly byte[] ReplacedInstructions = [0x8B, 0x45, 0x08, 0xB9, 0x08, 0x00, 0x00, 0x00];

    private static readonly byte[] HandlerReturn = [0x33, 0xC0, 0x5F, 0x5E, 0x5B, 0x5D, 0xC2, 0x04, 0x00];

    public static uint Locate(CodeImage image) => image.FindSingle(HandlerStart, Name);

    public static byte[] Code(uint site, uint block, uint game)
    {
        byte[] skip = [0xFF, 0x05, .. Address(block + SkipsOffset), .. HandlerReturn];
        byte[] presenceCheck =
        [
            0x6B, 0xC0, SlotStride,
            0x83, 0xB8, .. Address(game + FirstPresenceField), SitsOut,
            0x75, (byte)skip.Length,
        ];
        byte[] gate =
        [
            0x8B, 0x43, SlotField,
            0x83, 0xF8, LastSlot,
            0x77, (byte)(presenceCheck.Length + skip.Length),
            .. presenceCheck,
            .. skip,
            .. ReplacedInstructions,
        ];
        var jumpEnd = block + CodeOffset + (uint)gate.Length + JumpLength;

        return [.. gate, JumpNear, .. Address(site + ReplacedLength - jumpEnd)];
    }

    public static byte[] Jump(uint site, uint block) =>
        [JumpNear, .. Address(block + CodeOffset - (site + JumpLength)), NoOperation, NoOperation, NoOperation];

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
