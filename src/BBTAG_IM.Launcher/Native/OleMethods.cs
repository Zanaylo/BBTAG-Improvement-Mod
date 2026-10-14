using System.Runtime.InteropServices;

namespace BBTAG_IM.Launcher.Native;

[StructLayout(LayoutKind.Explicit)]
internal struct Variant
{
    public const ushort Integer = 3;
    public const ushort Dispatch = 9;

    [FieldOffset(0)]
    public ushort Type;

    [FieldOffset(8)]
    public int IntegerValue;

    [FieldOffset(8)]
    public nint PointerValue;

    [FieldOffset(8)]
    public long Storage;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DispatchParameters
{
    public Variant* Arguments;
    public int* NamedArguments;
    public uint ArgumentCount;
    public uint NamedArgumentCount;
}

internal static unsafe partial class OleMethods
{
    public const uint MultiThreaded = 0;

    private const string Ole32 = "ole32.dll";
    private const string OleAut32 = "oleaut32.dll";

    [LibraryImport(Ole32)]
    public static partial int CoInitializeEx(nint reserved, uint concurrency);

    [LibraryImport(Ole32)]
    public static partial void CoUninitialize();

    [LibraryImport(Ole32)]
    public static partial int GetRunningObjectTable(uint reserved, out nint table);

    [LibraryImport(Ole32)]
    public static partial int CreateBindCtx(uint reserved, out nint context);

    [LibraryImport(OleAut32)]
    public static partial int VariantClear(Variant* variant);
}
