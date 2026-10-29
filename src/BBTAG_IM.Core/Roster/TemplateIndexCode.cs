using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Roster;

public static class TemplateIndexCode
{
    public const int MapOffset = 0;
    public const int MapLength = 64;
    public const int CodeOffset = MapLength;
    public const int BlockLength = 128;
    public const int JumpLength = 5;

    private const string Name = "Battle object character store";
    private const byte JumpNear = 0xE9;

    private static readonly BytePattern ObjectInit = BytePattern.Parse("8B 06 89 43 34 8B 46 08 83 F8 FF");

    public static uint Locate(CodeImage image) => image.FindSingle(ObjectInit, Name);

    public static byte[] Map(IEnumerable<RosterAddition> additions)
    {
        var map = Enumerable.Range(0, MapLength).Select(index => (byte)index).ToArray();

        foreach (var addition in additions)
            map[addition.Index] = (byte)addition.TemplateIndex;

        return map;
    }

    public static byte[] Code(uint site, uint block)
    {
        byte[] lookup =
        [
            0x8B, 0x06,
            0x83, 0xF8, MapLength,
            0x73, 0x07,
            0x0F, 0xB6, 0x80, .. Address(block + MapOffset),
            0x89, 0x43, 0x34,
        ];
        var jumpEnd = block + CodeOffset + (uint)lookup.Length + JumpLength;

        return [.. lookup, JumpNear, .. Address(site + JumpLength - jumpEnd)];
    }

    public static byte[] Jump(uint site, uint block) => [JumpNear, .. Address(block + CodeOffset - (site + JumpLength))];

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
