using System.Runtime.InteropServices;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Native;

namespace BBTAG_IM.Payload.Plugins;

internal static unsafe class PluginLoader
{
    public static void StartAll(IEnumerable<ModFolder> mods, string logDirectory, ILog log)
    {
        foreach (var mod in mods)
        {
            foreach (var pluginPath in mod.PluginPaths)
                TryStart(mod, pluginPath, logDirectory, log);
        }
    }

    private static void TryStart(ModFolder mod, string pluginPath, string logDirectory, ILog log)
    {
        var name = $"{mod.Name}\\{Path.GetFileName(pluginPath)}";

        try
        {
            Start(mod, pluginPath, name, logDirectory, log);
        }
        catch (Exception exception) when (exception is InvalidPeImageException or IOException)
        {
            log.Error($"{name} could not be read: {exception.Message}");
        }
    }

    private static void Start(ModFolder mod, string pluginPath, string name, string logDirectory, ILog log)
    {
        if (ModPlugin.FindStart(FileImage.Load(pluginPath)) is null)
        {
            log.Info($"{name} has no {ModPlugin.StartExport} export, so it is not loaded.");
            return;
        }

        var module = Kernel32.LoadLibrary(pluginPath);

        if (module == 0)
        {
            log.Error($"{name} could not be loaded (Windows error {Marshal.GetLastPInvokeError()}).");
            return;
        }

        var start = (delegate* unmanaged[Stdcall]<char*, char*, uint>)Kernel32.GetProcAddress(module, ModPlugin.StartExport);
        uint status;

        fixed (char* modFolder = mod.RootPath)
        fixed (char* logFolder = logDirectory)
            status = start(modFolder, logFolder);

        if (status != ModPlugin.Started)
        {
            log.Error($"{name} failed to start (status {status}). Its log is in {logDirectory}.");
            return;
        }

        log.Info($"{name} started.");
    }
}
