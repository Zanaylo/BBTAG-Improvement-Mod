using System.Globalization;
using System.Text;
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

    private const byte MoveToEax = 0xB8;
    private const int GetterLength = 5;
    private const uint RecordLength = 0xB8;
    private const uint ShortNameOffset = 0x40;
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

    private uint? Record(int index)
    {
        if (index < 0 || getter is not { } address || !memory.CanRead(address, GetterLength))
            return null;

        Span<byte> code = stackalloc byte[GetterLength];
        memory.Read(address, code);

        if (code[0] != MoveToEax)
            return null;

        var record = memory.ReadUInt32(address + 1) + (uint)index * RecordLength;
        return memory.CanRead(record, (int)RecordLength) ? record : null;
    }

    private string? Code(uint record) => Text(record) is var code && Characters.IsCode(code) ? code : null;

    private string ShortName(uint record)
    {
        var shortName = Text(record + ShortNameOffset);
        var words = shortName.IndexOf(WordSeparator) == GamePrefixLength ? shortName[(GamePrefixLength + 1)..] : shortName;

        return Words.ToTitleCase(words.Replace(WordSeparator, ' ').ToLowerInvariant()).Trim();
    }

    private string Text(uint address)
    {
        Span<byte> bytes = stackalloc byte[TextLength];
        memory.Read(address, bytes);
        var terminator = bytes.IndexOf((byte)0);

        return Encoding.Latin1.GetString(terminator < 0 ? bytes : bytes[..terminator]);
    }

    private static CharacterIdentity Numbered(int index) => new(index, $"{Characters.NumberedPrefix}{index}", $"{Characters.NumberedPrefix}{index}");
}
