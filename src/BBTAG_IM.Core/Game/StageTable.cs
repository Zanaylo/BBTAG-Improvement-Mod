using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public sealed class StageTable(IGameMemory memory, uint? getter)
{
    private static readonly SingletonPattern GetterPattern = new(
        "Stage table",
        BytePattern.Parse("6A 01 FF B3 4C 28 00 00 6A 00 E8"),
        CallOffset: 10);

    private const uint RecordLength = 0xC4;
    private const int NameLength = 0x20;
    private const int NoStageChosen = 0x200;

    public static uint LocateGetter(CodeImage image) => new SingletonLocator(image).LocateGetter(GetterPattern);

    public int? IndexOf(string name)
    {
        for (var index = 0; index < NoStageChosen; index++)
        {
            var stageName = NameOf(index);

            if (stageName is null)
                return null;

            if (stageName == name)
                return index;
        }

        return null;
    }

    public string? NameOf(int index)
    {
        if (index < 0 || getter is not { } address || memory.AddressLoadedBy(address) is not { } table)
            return null;

        var record = table + (uint)index * RecordLength;
        return memory.CanRead(record, NameLength) ? memory.ReadText(record, NameLength) : null;
    }
}
