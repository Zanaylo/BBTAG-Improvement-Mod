using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Training;

public static class PauseGateCode
{
    public const int HoldOffset = 0;
    public const int CallsOffset = 4;
    public const int CodeOffset = 8;
    public const int BlockLength = 32;
    public const int JumpLength = 5;

    private const string Name = "Match update check";
    private const uint ZeroReturnOffset = 24;
    private const byte IncrementPrefix = 0xFF;
    private const byte IncrementMemory = 0x05;
    private const byte LoadEax = 0xA1;
    private const byte Return = 0xC3;
    private const byte JumpNear = 0xE9;

    private static readonly BytePattern UpdateCheckEnd =
        BytePattern.Parse("83 3D ?? ?? ?? ?? 00 74 0F 83 3D ?? ?? ?? ?? 00 75 06 B8 01 00 00 00 C3 33 C0 C3 CC CC");

    public static uint Locate(CodeImage image) => image.FindSingle(UpdateCheckEnd, Name) + ZeroReturnOffset;

    public static byte[] Code(uint block) =>
        [IncrementPrefix, IncrementMemory, .. Address(block + CallsOffset), LoadEax, .. Address(block + HoldOffset), Return];

    public static byte[] Jump(uint site, uint block) => [JumpNear, .. Address(block + CodeOffset - (site + JumpLength))];

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
