namespace BBTAG_IM.Launcher.Debugging;

internal unsafe class ComObject(nint instance) : IDisposable
{
    private const int QueryInterfaceSlot = 0;
    private const int ReleaseSlot = 2;

    private nint _instance = instance;

    protected nint Instance => _instance;

    public nint QueryInterface(Guid interfaceId)
    {
        nint result;
        var query = (delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Method(QueryInterfaceSlot);
        AutomationException.ThrowIfFailed(query(_instance, &interfaceId, &result));
        return result;
    }

    public void Dispose()
    {
        if (_instance == 0)
            return;

        ((delegate* unmanaged[Stdcall]<nint, uint>)Method(ReleaseSlot))(_instance);
        _instance = 0;
    }

    protected nint Method(int slot) => (*(nint**)_instance)[slot];
}
