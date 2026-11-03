using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.ImprovementMod.Dev;

internal sealed unsafe class GameBoxReaders : IBoxReaders
{
    private const uint ReaderField = 0x3C;
    private const uint LoadSlot = sizeof(uint);
    private const uint ShiftField = 0xC0;
    private const int ShiftLength = 2 * sizeof(int);

    private readonly IGameMemory _memory;
    private readonly uint _loadBoxes;
    private readonly delegate* unmanaged[Thiscall]<nint, nint, int> _load;

    public GameBoxReaders(IGameMemory memory, uint loadBoxes)
    {
        _memory = memory;
        _loadBoxes = loadBoxes;
        _load = (delegate* unmanaged[Thiscall]<nint, nint, int>)loadBoxes;
    }

    public void Load(uint entity, uint jonbin)
    {
        var reader = entity + ReaderField;

        if (_memory.ReadPointer(reader) is not { } table || _memory.ReadPointer(table + LoadSlot) != _loadBoxes)
            return;

        var shift = _memory.ReadBytes(reader + ShiftField, ShiftLength);
        _load((nint)reader, (nint)jonbin);
        _memory.Write(reader + ShiftField, shift);
    }
}
