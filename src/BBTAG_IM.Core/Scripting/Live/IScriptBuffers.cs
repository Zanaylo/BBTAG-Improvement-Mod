namespace BBTAG_IM.Core.Scripting.Live;

public interface IScriptBuffers
{
    uint Allocate(byte[] bytes);

    void Release(uint address);
}
