using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Improvement;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Updates;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Standalone;

public static unsafe class StandaloneExports
{
    private const string StartExport = "BBTAG_IM_StandaloneStart";
    private const string CreateExport = "DirectInput8Create";
    private const string SystemLibraryName = "dinput8.dll";
    private const int CouldNotCreate = unchecked((int)0x80004005);

    private static delegate* unmanaged[Stdcall]<nint, uint, nint, nint*, nint, int> s_create;

    [UnmanagedCallersOnly(EntryPoint = StartExport, CallConvs = [typeof(CallConvStdcall)])]
    public static uint Start(nint unused)
    {
        try
        {
            var layout = StandaloneLayout.For(LoadedModule.MainModule().Path, new PhysicalFileSystem());
            Directory.CreateDirectory(layout.ModFolder);
            UpdaterStarter.TryStartAfter(layout.GameFolder, Environment.ProcessId);
            return ImprovementStartup.Run(layout.ModFolder, layout.LogFolder);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return ModPlugin.Failed;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = CreateExport, CallConvs = [typeof(CallConvStdcall)])]
    public static int DirectInput8Create(nint instance, uint version, nint interfaceId, nint* created, nint outer)
    {
        if (s_create is null)
            s_create = LoadSystemCreate();

        return s_create is null ? CouldNotCreate : s_create(instance, version, interfaceId, created, outer);
    }

    private static delegate* unmanaged[Stdcall]<nint, uint, nint, nint*, nint, int> LoadSystemCreate()
    {
        var library = Kernel32.LoadLibrary(Path.Combine(Environment.SystemDirectory, SystemLibraryName));

        return library == 0
            ? null
            : (delegate* unmanaged[Stdcall]<nint, uint, nint, nint*, nint, int>)Kernel32.GetProcAddress(library, CreateExport);
    }
}
