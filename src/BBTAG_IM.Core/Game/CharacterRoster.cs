using System.Globalization;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Game;

public sealed record CharacterIdentity(int Index, string Folder, string Name);

public sealed class CharacterRoster(IGameMemory memory, uint? getter)
{
    private static readonly SingletonPattern GetterPattern = new(
        "Character roster",
        BytePattern.Parse("33 FF E8 ?? ?? ?? ?? 8B 80 00 2E 00 00 83 C0 01 0F 84"),
        CallOffset: 2);

    private const uint RecordLength = 0xB8;
    private const uint ShortNameOffset = 0x40;
    private const uint SpriteFolderOffset = 0xB0;
    private const uint LastIndexOffset = 0x2E00;
    private const int TextLength = 0x20;
    private const char WordSeparator = '_';
    private const int GamePrefixLength = 1;

    private static readonly TextInfo Words = CultureInfo.InvariantCulture.TextInfo;

    public static uint LocateGetter(CodeImage image) => new SingletonLocator(image).LocateGetter(GetterPattern);

    public CharacterIdentity Identify(int index)
    {
        if (Characters.NameOf(index) is { } name)
            return new CharacterIdentity(index, name, name);

        if (Record(index) is not { } record || Code(record) is not { } code)
            return Numbered(index);

        return ShortName(record) is { Length: > 0 } shortName
            ? new CharacterIdentity(index, code, $"{shortName} ({code})")
            : new CharacterIdentity(index, code, code);
    }

    public string? CodeOf(int index) => Record(index) is { } record ? Code(record) : null;

    public int? LastIndex =>
        Table() is { } table && memory.CanRead(table + LastIndexOffset, sizeof(int))
            ? memory.ReadInt32(table + LastIndexOffset)
            : null;

    public bool Contains(int index) => index >= 0 && index <= LastIndex;

    public int? IndexOf(string code)
    {
        if (LastIndex is not { } lastIndex)
            return null;

        for (var index = 0; index <= lastIndex; index++)
        {
            if (Record(index) is { } record && Code(record) == code)
                return index;
        }

        return null;
    }

    public bool EnsureSpriteFolder(int index, SpriteFolder folder)
    {
        if (Record(index) is not { } record)
            return false;

        var field = record + SpriteFolderOffset;

        if (memory.ReadInt32(field) == (int)folder || !memory.CanWrite(field, sizeof(int)))
            return false;

        Span<byte> value = stackalloc byte[sizeof(int)];
        BitConverter.TryWriteBytes(value, (int)folder);
        memory.Write(field, value);
        return true;
    }

    private uint? Record(int index)
    {
        if (index < 0 || Table() is not { } table)
            return null;

        var record = table + (uint)index * RecordLength;
        return memory.CanRead(record, (int)RecordLength) ? record : null;
    }

    private uint? Table() => getter is { } address ? memory.AddressLoadedBy(address) : null;

    private string? Code(uint record) => Text(record) is var code && Characters.IsCode(code) ? code : null;

    private string ShortName(uint record)
    {
        var shortName = Text(record + ShortNameOffset);
        var words = shortName.IndexOf(WordSeparator) == GamePrefixLength ? shortName[(GamePrefixLength + 1)..] : shortName;

        return Words.ToTitleCase(words.Replace(WordSeparator, ' ').ToLowerInvariant()).Trim();
    }

    private string Text(uint address) => memory.ReadText(address, TextLength);

    private static CharacterIdentity Numbered(int index) => new(index, $"{Characters.NumberedPrefix}{index}", $"{Characters.NumberedPrefix}{index}");
}
