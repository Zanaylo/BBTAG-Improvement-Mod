using System.Runtime.InteropServices;

namespace BBTAG_IM.Native;

[StructLayout(LayoutKind.Sequential)]
public struct Variant
{
    public ushort Type;
    public ushort Reserved1;
    public ushort Reserved2;
    public ushort Reserved3;
    public nint Value;
    public nint Extra;
}

[StructLayout(LayoutKind.Sequential)]
public struct DispatchParameters
{
    public nint Arguments;
    public nint NamedArguments;
    public uint Count;
    public uint NamedCount;
}

public static partial class OleAut32
{
    public const ushort TextType = 8;
    public const ushort ObjectType = 9;
    public const ushort MethodCall = 1;
    public const ushort PropertyGet = 2;

    private const string Library = "oleaut32.dll";

    [LibraryImport(Library, EntryPoint = "SysAllocString", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint AllocateText(string text);

    [LibraryImport(Library)]
    public static partial int VariantClear(ref Variant variant);
}
