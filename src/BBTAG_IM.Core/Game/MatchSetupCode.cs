using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public static class MatchSetupCode
{
    public const int CharactersOffset = 0;
    public const int ColorsOffset = 16;
    public const int PresencesOffset = 32;
    public const int StageOffset = 48;
    public const int CountOffset = 52;
    public const int CodeOffset = 64;
    public const int BlockLength = 512;
    public const int JumpLength = 5;

    private const string Name = "Match setup";
    private const uint CallOffset = 13;
    private const uint FirstCharacterField = 0x27A8;
    private const uint FirstColorField = 0x27B0;
    private const uint FirstPresenceField = 0x27C4;
    private const uint SlotStride = 0x28;
    private const uint StageField = 0x284C;
    private const byte CallNear = 0xE8;
    private const byte JumpNear = 0xE9;

    private static readonly BytePattern MusicStoreEnd = BytePattern.Parse("3D 00 02 00 00 74 06 89 86 50 28 00 00 E8");

    public static uint Locate(CodeImage image) => image.FindSingle(MusicStoreEnd, Name) + CallOffset;

    public static uint CalledBy(CodeImage image, uint site) => image.CallTarget(site, Name);

    public static byte[] Code(uint site, uint block, uint called)
    {
        byte[] choose =
        [
            .. Enumerable.Range(0, MatchSlots.Count).SelectMany(slot => SlotOverrides(block, (uint)slot)),
            .. Override(block + StageOffset, StageField),
            0xFF, 0x05, .. Address(block + CountOffset),
        ];
        var callEnd = block + CodeOffset + (uint)choose.Length + JumpLength;

        return [.. choose, CallNear, .. Address(called - callEnd), JumpNear, .. Address(site + JumpLength - (callEnd + JumpLength))];
    }

    public static byte[] Jump(uint site, uint block) => [JumpNear, .. Address(block + CodeOffset - (site + JumpLength))];

    private static byte[] SlotOverrides(uint block, uint slot) =>
    [
        .. Override(block + CharactersOffset + slot * sizeof(int), FirstCharacterField + slot * SlotStride),
        .. Override(block + ColorsOffset + slot * sizeof(int), FirstColorField + slot * SlotStride),
        .. Override(block + PresencesOffset + slot * sizeof(int), FirstPresenceField + slot * SlotStride),
    ];

    private static byte[] Override(uint source, uint gameField) =>
    [
        0xA1, .. Address(source),
        0x83, 0xF8, 0xFF,
        0x74, 0x06,
        0x89, 0x86, .. Address(gameField),
    ];

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
