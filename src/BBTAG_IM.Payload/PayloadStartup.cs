using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Patching;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Payload.Hooks;
using BBTAG_IM.Payload.Native;
using BBTAG_IM.Payload.Patching;

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
        PatchExecutable(mods, game, log);
        RedirectModFiles(mods, game, session.GameDirectory, fileSystem, log);
    }

    private static void PatchExecutable(IReadOnlyList<ModFolder> mods, LoadedModule game, ILog log)
    {
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

        FileApiHooks.Install(game, new ModFileRedirector(index, gameDirectory), log);
    }
}
