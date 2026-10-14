using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Patching;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;
using BBTAG_IM.Payload.Hooks;
using BBTAG_IM.Payload.Patching;
using BBTAG_IM.Payload.Plugins;

namespace BBTAG_IM.Payload;

internal static class PayloadStartup
{
    public static PayloadStatus Run(string sessionText)
    {
        LaunchSession session;

        try
        {
            session = LaunchSession.Parse(sessionText);
        }
        catch (InvalidLaunchSessionException)
        {
            return PayloadStatus.InvalidSession;
        }

        var log = new FileLog(session.LogPath, TimeProvider.System);

        try
        {
            Start(session, log);
            log.Info("Ready. The game is starting.");
            return PayloadStatus.Ready;
        }
        catch (Exception exception)
        {
            log.Error(exception is InvalidModFolderException or IncompatibleExecutableException or PatchFailedException or HookException
                ? exception.Message
                : exception.ToString());
            return StatusFor(exception);
        }
    }

    private static PayloadStatus StatusFor(Exception exception) => exception switch
    {
        InvalidModFolderException => PayloadStatus.InvalidModFolder,
        IncompatibleExecutableException => PayloadStatus.IncompatibleExecutable,
        PatchFailedException => PayloadStatus.PatchFailed,
        HookException => PayloadStatus.HookFailed,
        _ => PayloadStatus.Unexpected,
    };

    private static void Start(LaunchSession session, ILog log)
    {
        var fileSystem = new PhysicalFileSystem();
        var mods = session.ModFolders.Select(folder => ModFolder.Inspect(folder, fileSystem)).ToArray();
        var game = LoadedModule.MainModule();

        log.Info($"Payload loaded into {game.Path} at 0x{game.BaseAddress:X8}.");
        WaitForDebugger(session, log);
        ExitHooks.Install(game, log);
        PatchExecutable(mods, game, session.GameDirectory, log);
        RedirectModFiles(mods, game, session.GameDirectory, fileSystem, log);
        PluginLoader.StartAll(mods, Path.GetDirectoryName(session.LogPath) ?? session.GameDirectory, log);
    }

    private static void WaitForDebugger(LaunchSession session, ILog log)
    {
        if (!session.WaitForDebugger)
            return;

        log.Info($"Waiting up to {DebuggerWait.Limit.TotalMinutes:0} minutes for a debugger. Attach to BBTAG.exe (process {Environment.ProcessId}) with Native code.");
        var attached = new DebuggerWait(Kernel32.IsDebuggerPresent, TimeProvider.System, Thread.Sleep).Wait();
        log.Info(attached ? "A debugger is attached. Loading the mods." : "No debugger was attached. Loading the mods anyway.");
    }

    private static void PatchExecutable(IReadOnlyList<ModFolder> mods, LoadedModule game, string gameDirectory, ILog log)
    {
        if (!LaunchTarget.IsInstalledGame(game.Path, gameDirectory))
        {
            log.Info($"Running {game.Path} as it is, so its native code is used.");
            return;
        }

        var executableMods = mods.Where(mod => mod.ExecutablePath is not null).ToArray();

        if (executableMods.Length == 0)
        {
            log.Info("No mod ships a BBTAG.exe, so the game code stays as it is.");
            return;
        }

        if (executableMods.Length > 1)
            log.Info($"Several mods ship a BBTAG.exe. Using {executableMods[0].Name}; ignoring {string.Join(", ", executableMods.Skip(1).Select(mod => mod.Name))}.");

        var plan = ExePatchPlanner.Plan(FileImage.Load(game.Path), FileImage.Load(executableMods[0].ExecutablePath!));
        log.Info($"{executableMods[0].Name}: BBTAG.exe differs from the installed game in {plan.Writes.Count} place(s) and adds {plan.Sections.Count} section(s).");
        MemoryPatcher.Apply(plan, game, log);
    }

    private static void RedirectModFiles(IReadOnlyList<ModFolder> mods, LoadedModule game, string gameDirectory, IFileSystem fileSystem, ILog log)
    {
        var index = ModFileIndex.Build(mods, fileSystem);

        foreach (var (mod, files) in index.FilesByMod)
            log.Info($"{mod}: {files} file(s) layered over the game data.");

        var executableDirectory = Path.GetDirectoryName(game.Path) ?? gameDirectory;
        var redirector = new ModFileRedirector(index, gameDirectory, executableDirectory);
        FileApiHooks.Install(game, WithDevFolder(redirector, index, gameDirectory, executableDirectory, fileSystem, log), log);
    }

    private static ModFileRedirector WithDevFolder(
        ModFileRedirector redirector,
        ModFileIndex index,
        string gameDirectory,
        string executableDirectory,
        IFileSystem fileSystem,
        ILog log)
    {
        var devFolder = Path.Combine(gameDirectory, ModSelection.ModsFolderName, DevFolder.Name);
        var launcher = SettingsStores.Launcher(fileSystem, gameDirectory).TryLoad() ?? new LauncherSettings();

        if (!launcher.IsModEnabled(DevFolder.Name))
        {
            log.Info($"The {DevFolder.Name} folder is turned off in the Mod Loader, so its files are not read live.");
            return redirector;
        }

        log.Info($"Files in {devFolder} are read live and win over every other mod. A folder named like an archive is packed into it when the game opens it.");
        return new ModFileRedirector(index, gameDirectory, executableDirectory, new DevOverlay(fileSystem, devFolder, redirector.ResolveBelowDev));
    }
}
