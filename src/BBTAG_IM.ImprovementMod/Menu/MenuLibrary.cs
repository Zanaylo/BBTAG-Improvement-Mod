using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Menu;

internal static class MenuLibrary
{
    public static bool Load(string modFolder) =>
        Unpack(modFolder) is { } path && Kernel32.LoadLibrary(path) != 0;

    private static string? Unpack(string modFolder)
    {
        using var library = typeof(MenuLibrary).Assembly.GetManifestResourceStream(MenuOverlay.NativeLibraryName);

        if (library is null)
            return null;

        try
        {
            return new BundledFiles(new PhysicalFileSystem(), modFolder).Place(MenuOverlay.NativeLibraryName, library);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
