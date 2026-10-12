namespace BBTAG_IM.Native.Hooks;

public static unsafe class VirtualTable
{
    public static nint Read(nint comObject, int slot) => *(nint*)(*(nint*)comObject + slot * sizeof(nint));

    public static nint Replace(nint comObject, int slot, nint replacement)
    {
        var table = *(nint*)comObject;
        return ProtectedMemory.SwapPointer(table + slot * sizeof(nint), replacement);
    }
}
