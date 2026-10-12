using BBTAG_IM.Core.Pe;

namespace BBTAG_IM.Native.Hooks;

public static class ImportHook
{
    public static nint Replace(LoadedModule module, string dllName, string functionName, nint replacement)
    {
        var slot = ImportTable.FindSlotRva(module, module.Headers, dllName, functionName)
            ?? throw new HookException($"The game does not import {functionName} from {dllName}.");

        return ProtectedMemory.SwapPointer(module.AddressOf(slot), replacement);
    }
}
