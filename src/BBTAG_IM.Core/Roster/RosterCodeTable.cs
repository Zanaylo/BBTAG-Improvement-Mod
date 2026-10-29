using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Roster;

public static class RosterCodeTable
{
    private const string Name = "Character roster codes";
    private const uint TableOperand = 3;
    private const int TextLength = 0x20;
    private const int MaximumCodes = 64;

    private static readonly BytePattern CodeRead = BytePattern.Parse("8B 0C 9D ?? ?? ?? ?? 85 C9 0F 84");

    public static IReadOnlyList<string> Read(CodeImage image, IGameMemory memory)
    {
        var site = image.FindSingle(CodeRead, Name);
        var table = BitConverter.ToUInt32(image.Slice(site + TableOperand, sizeof(uint)));
        List<string> codes = [];

        while (codes.Count < MaximumCodes && memory.ReadPointer(table + (uint)(codes.Count * sizeof(uint))) is { } text)
            codes.Add(memory.ReadText(text, TextLength));

        return codes;
    }
}
