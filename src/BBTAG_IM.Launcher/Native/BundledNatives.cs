using System.Runtime.InteropServices;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;

namespace BBTAG_IM.Launcher.Native;

internal static class BundledNatives
{
    private const string WindowLibraryPrefix = "window/";
    private const string PayloadPrefix = "payload/";

    public static void Unpack(string launcherDirectory)
    {
        var bundle = new BundledFiles(new PhysicalFileSystem(), launcherDirectory);

        foreach (var library in Resources(WindowLibraryPrefix))
        {
            if (TryPlace(bundle, library) is { } path)
                NativeLibrary.TryLoad(path, out _);
        }

        foreach (var payload in Resources(PayloadPrefix))
            TryPlace(bundle, payload);
    }

    private static IEnumerable<BundledResource> Resources(string prefix) =>
        typeof(BundledNatives).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(name => new BundledResource(name, name[prefix.Length..]));

    private static string? TryPlace(BundledFiles bundle, BundledResource resource)
    {
        try
        {
            using var stream = typeof(BundledNatives).Assembly.GetManifestResourceStream(resource.Name)!;
            return bundle.Place(resource.FileName, stream);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed record BundledResource(string Name, string FileName);
}
