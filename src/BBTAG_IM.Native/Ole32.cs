using System.Runtime.InteropServices;

namespace BBTAG_IM.Native;

public static partial class Ole32
{
    private const string Library = "ole32.dll";

    [LibraryImport(Library)]
    public static partial int GetRunningObjectTable(uint reserved, out nint table);

    [LibraryImport(Library)]
    public static partial int CreateBindCtx(uint reserved, out nint context);
}
