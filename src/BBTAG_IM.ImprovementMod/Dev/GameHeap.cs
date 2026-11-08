using BBTAG_IM.Core.Memory;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Dev;

internal sealed unsafe class GameHeap(uint allocator) : IGameHeap
{
    public uint Allocate(int length)
    {
        var block = ((delegate* unmanaged[Cdecl]<nuint, nint>)allocator)((nuint)length);

        if (block == 0)
            throw new PatchFailedException($"The game could not give {length} bytes of memory.");

        return (uint)block;
    }
}
