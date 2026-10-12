using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Launcher.Injection;

namespace BBTAG_IM.Launcher;

internal static class Program
{
    private const int Success = 0;
    private const int UserError = 1;
    private const int PayloadFailure = 2;
    private const int UnexpectedFailure = 3;

    private const string Usage = """
        Usage: BBTAG_IM_Launcher.exe [mod folder ...] [--mod <folder>] [--game <folder>]

          <mod folder>      A mod to load. Drag one or more folders onto the launcher to load them.
                            Earlier folders win when two mods replace the same file.
          --mod, -m         Same as passing the folder directly.
          --game, -g        The game folder. Found through Steam when left out.
          --help, -h        Shows this text.

        Without mod folders, every folder inside 'mods' next to the launcher is loaded, in name order.
        A mod folder holds a 'data' or 'data2' folder laid out like the game's 'data' folder,
        and may hold a modded BBTAG.exe, whose changes are applied in memory.
        Nothing in the game folder is changed.
        """;

    private static int Main(string[] arguments)
    {
        Console.WriteLine($"BBTAG Improvement Mod launcher {typeof(Program).Assembly.GetName().Version?.ToString(3)}");
        var launcherDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);

        try
        {
            var options = LaunchOptions.Parse(arguments);

            if (!options.ShowHelp)
                return Run(options, launcherDirectory);

            Console.WriteLine(Usage);
            return Success;
        }
        catch (Exception exception) when (exception is LaunchOptionsException or GameNotFoundException or InvalidModFolderException or LaunchFailedException)
        {
            return Fail(exception.Message, UserError);
        }
        catch (Exception exception)
        {
            return Fail(exception.ToString(), UnexpectedFailure);
        }
    }

    private static int Run(LaunchOptions options, string launcherDirectory)
    {
        var fileSystem = new PhysicalFileSystem();
        var gameDirectory = new GameLocator(fileSystem, new SteamRegistry()).Locate(options.GameDirectory, launcherDirectory);
        var mods = ModSelection.Resolve(options, launcherDirectory, fileSystem);
        var logPath = Path.Combine(launcherDirectory, PayloadContract.LogFolder, PayloadContract.LogFileName);

        if (File.Exists(logPath))
            File.Delete(logPath);

        var log = new FileLog(logPath, TimeProvider.System);
        Describe(gameDirectory, mods, log);

        var session = new LaunchSession(gameDirectory, logPath, mods.Select(mod => mod.RootPath).ToArray());
        var injector = new PayloadInjector(Path.Combine(launcherDirectory, PayloadContract.DllFileName));
        var status = new GameLauncher(injector, log).Launch(session);

        if (status != PayloadStatus.Ready)
            return Fail($"{PayloadStatusText.Describe(status)} The game was closed. Details: {logPath}", PayloadFailure);

        Console.WriteLine($"Game started. Log: {logPath}");
        return Success;
    }

    private static void Describe(string gameDirectory, IReadOnlyList<ModFolder> mods, ILog log)
    {
        Report($"Game: {gameDirectory}", log);

        foreach (var mod in mods)
        {
            var parts = mod.DataRoots.Select(root => $"{Path.GetFileName(root.Path)} folder")
                .Concat(mod.ExecutablePath is null ? [] : [$"{ModFolder.GameExecutableName} changes"]);
            Report($"Mod:  {mod.Name} ({string.Join(", ", parts)})", log);
        }
    }

    private static void Report(string message, ILog log)
    {
        Console.WriteLine(message);
        log.Info(message);
    }

    private static int Fail(string message, int exitCode)
    {
        Console.Error.WriteLine(message);

        if (Console.IsInputRedirected)
            return exitCode;

        Console.WriteLine("Press Enter to close.");
        Console.ReadLine();
        return exitCode;
    }
}
