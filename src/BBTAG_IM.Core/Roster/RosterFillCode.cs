using System.Text;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Roster;

public static class RosterFillCode
{
    public const int NamesOffset = 0;
    public const int NameBlockLength = 0x80;
    public const int MaximumAdditions = 8;
    public const int NamesLength = NameBlockLength * MaximumAdditions;
    public const int CodeOffset = NamesLength;
    public const int BlockLength = 0x800;
    public const int JumpLength = 5;
    public const int ReplacedLength = 10;

    private const string Name = "Character roster fill";
    private const int TextLength = 0x20;
    private const uint RecordLength = 0xB8;
    private const uint SpriteFolderField = 0xB0;
    private const byte JumpNear = 0xE9;
    private const byte NoOperation = 0x90;

    private static readonly BytePattern FillEnd = BytePattern.Parse("C7 86 04 2F 00 00 00 00 00 00 8D 9E 04 2E 00 00");
    private static readonly byte[] ReplacedStore = [0xC7, 0x86, 0x04, 0x2F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    private static readonly byte[] CountIncrement = [0xFF, 0x86, 0x00, 0x2E, 0x00, 0x00];

    public static uint Locate(CodeImage image) => image.FindSingle(FillEnd, Name);

    public static byte[] Names(IReadOnlyList<RosterAddition> additions)
    {
        var names = new byte[NamesLength];

        for (var position = 0; position < additions.Count; position++)
        {
            var manifest = additions[position].Manifest;
            string[] texts = [manifest.Code, manifest.Code.ToUpperInvariant(), manifest.ShortName, manifest.Code];

            for (var field = 0; field < texts.Length; field++)
                Text(texts[field]).CopyTo(names, position * NameBlockLength + field * TextLength);
        }

        return names;
    }

    public static byte[] Code(uint site, uint block, IReadOnlyList<RosterAddition> additions)
    {
        byte[] fill =
        [
            0x60,
            .. additions.SelectMany((addition, position) => Fill(addition, block + NamesOffset + (uint)(position * NameBlockLength))),
            0x61,
            .. ReplacedStore,
        ];
        var jumpEnd = block + CodeOffset + (uint)fill.Length + JumpLength;

        return [.. fill, JumpNear, .. Address(site + ReplacedLength - jumpEnd)];
    }

    public static byte[] Jump(uint site, uint block) =>
        [JumpNear, .. Address(block + CodeOffset - (site + JumpLength)), .. Enumerable.Repeat(NoOperation, ReplacedLength - JumpLength)];

    private static byte[] Fill(RosterAddition addition, uint names)
    {
        var record = (uint)addition.Index * RecordLength;

        return
        [
            0x8D, 0xBE, .. Address(record),
            0x56,
            0x8D, 0xB6, .. Address((uint)addition.TemplateIndex * RecordLength),
            0xB9, .. Address(RecordLength / sizeof(uint)),
            0xF3, 0xA5,
            0x5E,
            0x8D, 0xBE, .. Address(record),
            0x56,
            0xBE, .. Address(names),
            0xB9, .. Address(NameBlockLength / sizeof(uint)),
            0xF3, 0xA5,
            0x5E,
            .. SpriteFolderStore(addition, record),
            .. CountIncrement,
        ];
    }

    private static byte[] SpriteFolderStore(RosterAddition addition, uint record) =>
        addition.Manifest.SpriteFolder is { } folder
            ? [0xC7, 0x86, .. Address(record + SpriteFolderField), .. BitConverter.GetBytes((int)folder)]
            : [];

    private static byte[] Text(string text)
    {
        var bytes = new byte[TextLength];
        Encoding.ASCII.GetBytes(text.Length < TextLength ? text : text[..(TextLength - 1)]).CopyTo(bytes, 0);
        return bytes;
    }

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
