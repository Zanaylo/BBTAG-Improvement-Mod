using System.Runtime.InteropServices;
using BBTAG_IM.Launcher.Native;

namespace BBTAG_IM.Launcher.Debugging;

internal sealed unsafe class RunningObjectTable : ComObject
{
    private const int GetObjectSlot = 6;
    private const int EnumRunningSlot = 9;
    private const int NextSlot = 3;
    private const int GetDisplayNameSlot = 20;
    private const int Succeeded = 0;

    private RunningObjectTable(nint instance)
        : base(instance)
    {
    }

    public static RunningObjectTable Open()
    {
        AutomationException.ThrowIfFailed(OleMethods.GetRunningObjectTable(0, out var table));
        return new RunningObjectTable(table);
    }

    public IReadOnlyList<DispatchObject> ObjectsNamed(Func<string, bool> wanted)
    {
        var found = new List<DispatchObject>();
        nint monikers;
        AutomationException.ThrowIfFailed(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(EnumRunningSlot))(Instance, &monikers));
        using var enumerator = new ComObject(monikers);
        AutomationException.ThrowIfFailed(OleMethods.CreateBindCtx(0, out var context));
        using var bindContext = new ComObject(context);

        while (Next(monikers) is { } moniker)
        {
            using var owned = new ComObject(moniker);

            if (DisplayNameOf(moniker, context) is { } name && wanted(name))
                found.Add(DispatchOf(moniker));
        }

        return found;
    }

    private DispatchObject DispatchOf(nint moniker)
    {
        nint unknown;
        AutomationException.ThrowIfFailed(((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)Method(GetObjectSlot))(Instance, moniker, &unknown));
        using var found = new ComObject(unknown);
        return new DispatchObject(found.QueryInterface(DispatchObject.InterfaceId));
    }

    private static nint? Next(nint monikers)
    {
        nint moniker;
        uint fetched;
        var next = (delegate* unmanaged[Stdcall]<nint, uint, nint*, uint*, int>)(*(nint**)monikers)[NextSlot];
        return next(monikers, 1, &moniker, &fetched) == Succeeded && fetched == 1 ? moniker : null;
    }

    private static string? DisplayNameOf(nint moniker, nint context)
    {
        char* name = null;
        var getDisplayName = (delegate* unmanaged[Stdcall]<nint, nint, nint, char**, int>)(*(nint**)moniker)[GetDisplayNameSlot];

        if (getDisplayName(moniker, context, 0, &name) < 0 || name == null)
            return null;

        try
        {
            return new string(name);
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)name);
        }
    }
}
