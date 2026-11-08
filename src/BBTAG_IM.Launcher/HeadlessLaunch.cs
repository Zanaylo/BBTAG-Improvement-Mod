using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Launcher.Ui;

namespace BBTAG_IM.Launcher;

internal static class HeadlessLaunch
{
    public const string Flag = "--play";

    private const int Started = 0;
    private const int Failed = 1;

    public static bool IsWanted(IReadOnlyList<string> arguments) => arguments.Contains(Flag, StringComparer.OrdinalIgnoreCase);

    public static int Run(IReadOnlyList<string> arguments)
    {
        var launcherDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);

        try
        {
            var fileSystem = new PhysicalFileSystem();
            var settings = SettingsStores.Launcher(fileSystem, launcherDirectory).Load();
            var modsDirectory = settings.ModsDirectory(launcherDirectory);
            var archives = new ModArchiveInstaller(fileSystem, new ArchiveUnpacker(), modsDirectory);
            var options = archives.WithArchivesInstalled(LaunchOptions.Parse(arguments.Where(argument => !argument.Equals(Flag, StringComparison.OrdinalIgnoreCase)).ToArray()));
            archives.InstallPending(_ => { });
            var mods = settings.InLoadOrder(ModSelection.Resolve(options, modsDirectory, fileSystem));
            var context = LaunchContext.Resolve(options, launcherDirectory, settings.EnabledOf(mods)) with { TablePath = null, Table = null };

            using var session = new GameSession(context, settings, new SilentView());
            session.RunAsync(() => []).GetAwaiter().GetResult();
            return Started;
        }
        catch (Exception exception) when (LaunchErrors.IsExpected(exception))
        {
            return Failed;
        }
    }

    private sealed class SilentView : ILauncherView
    {
        public void Report(string message)
        {
        }

        public void ShowStatus(string message)
        {
        }

        public void ShowActiveScripts(IReadOnlySet<int> activeScripts)
        {
        }
    }
}
