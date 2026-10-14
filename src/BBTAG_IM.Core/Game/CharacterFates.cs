using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public sealed class CharacterFates(IGameMemory memory, CharacterRoster roster, uint? fateFunction) : ICharacterFates
{
    private const int FunctionLength = 0x20;
    private const int TableOperandOffset = 3;

    private static readonly BytePattern TableLookup = BytePattern.Parse("0F B6 80 ?? ?? ?? ?? 5D C2 04 00");

    private static readonly Fate[] StockFates =
    [
        .. Enumerable.Repeat(Fate.BlazBlue, 16),
        .. Enumerable.Repeat(Fate.Persona, 10),
        .. Enumerable.Repeat(Fate.UnderNight, 10),
        .. Enumerable.Repeat(Fate.RWBY, 4),
        Fate.BlazBlue, Fate.Persona, Fate.UnderNight, Fate.ArcanaHeart, Fate.BlazBlue, Fate.BlazBlue, Fate.Persona, Fate.Persona,
        Fate.AkatsukiBlitzkampf, Fate.UnderNight, Fate.RWBY, Fate.SenranKagura, Fate.AkatsukiBlitzkampf,
    ];

    public Fate? Of(int characterIndex)
    {
        if (FateTable() is not { } table)
            return StockFateOf(characterIndex);

        if (!roster.Contains(characterIndex) || !memory.CanRead(table + (uint)characterIndex, sizeof(byte)))
            return null;

        Span<byte> value = stackalloc byte[sizeof(byte)];
        memory.Read(table + (uint)characterIndex, value);
        var fate = (Fate)value[0];
        return Enum.IsDefined(fate) ? fate : null;
    }

    private uint? FateTable()
    {
        if (fateFunction is not { } function || !memory.CanRead(function, FunctionLength))
            return null;

        var code = new byte[FunctionLength];
        memory.Read(function, code);
        var lookup = TableLookup.FindAll(code).FirstOrDefault(-1);

        return lookup < 0 ? null : memory.ReadUInt32(function + (uint)(lookup + TableOperandOffset));
    }

    private static Fate? StockFateOf(int characterIndex) =>
        characterIndex >= 0 && characterIndex < StockFates.Length ? StockFates[characterIndex] : null;
}
