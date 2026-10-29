using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Roster;

public static class CharacterGridCode
{
    public const int CodeOffset = 0;
    public const int BlockLength = 0x200;
    public const int JumpLength = 5;
    public const int ReplacedLength = 10;

    private const string Name = "Character select grid";
    private const uint FirstRow = 0x830;
    private const uint RowLength = 0x100;
    private const uint RowSlots = RowLength / sizeof(int);
    private const byte JumpNear = 0xE9;
    private const byte NoOperation = 0x90;

    private static readonly BytePattern BuilderEnd = BytePattern.Parse("C7 86 3C 0C 00 00 34 00 00 00 5E C3");
    private static readonly byte[] ReplacedStore = [0xC7, 0x86, 0x3C, 0x0C, 0x00, 0x00, 0x34, 0x00, 0x00, 0x00];

    public static uint Locate(CodeImage image) => image.FindSingle(BuilderEnd, Name);

    public static byte[] Code(uint site, uint block, IReadOnlyList<RosterAddition> additions)
    {
        byte[] append =
        [
            .. ReplacedStore,
            0x60,
            .. additions.SelectMany(Append),
            0x61,
        ];
        var jumpEnd = block + CodeOffset + (uint)append.Length + JumpLength;

        return [.. append, JumpNear, .. Address(site + ReplacedLength - jumpEnd)];
    }

    public static byte[] Jump(uint site, uint block) =>
        [JumpNear, .. Address(block + CodeOffset - (site + JumpLength)), .. Enumerable.Repeat(NoOperation, ReplacedLength - JumpLength)];

    private static byte[] Append(RosterAddition addition) =>
    [
        0x8D, 0xBE, .. Address(FirstRow + (uint)addition.Manifest.Series * RowLength),
        0xB8, 0xFF, 0xFF, 0xFF, 0xFF,
        0xB9, .. Address(RowSlots),
        0xF2, 0xAF,
        0x75, 0x07,
        0xC7, 0x47, 0xFC, .. BitConverter.GetBytes(addition.Index),
    ];

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
