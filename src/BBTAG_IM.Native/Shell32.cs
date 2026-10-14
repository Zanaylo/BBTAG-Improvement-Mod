using System.Runtime.InteropServices;

namespace BBTAG_IM.Native;

[StructLayout(LayoutKind.Sequential)]
public struct OpenAsInfo
{
    public nint File;
    public nint FileClass;
    public int Flags;
}

public static partial class Shell32
{
    public const int OpenWithExecute = 0x4;
    public const int OpenWithHideAlwaysUse = 0x20;
    public const int Cancelled = unchecked((int)0x800704C7);
    public const int EveryHandler = 0;

    private const string Library = "shell32.dll";

    [LibraryImport(Library, EntryPoint = "SHOpenWithDialog")]
    public static partial int OpenWithDialog(nint owner, ref OpenAsInfo information);

    [LibraryImport(Library, EntryPoint = "SHAssocEnumHandlers", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int EnumerateHandlers(string extension, int filter, out nint handlers);
}
