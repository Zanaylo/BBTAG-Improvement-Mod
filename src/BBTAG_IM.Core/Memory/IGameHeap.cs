namespace BBTAG_IM.Core.Memory;

public interface IGameHeap
{
    uint Allocate(int length);
}
