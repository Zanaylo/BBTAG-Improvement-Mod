using Avalonia;
using BBTAG_IM.Launcher.Native;
using BBTAG_IM.Launcher.Ui;

namespace BBTAG_IM.Launcher;

internal static class Program
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        BundledNatives.Unpack(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));

        return HeadlessLaunch.IsWanted(arguments) ? HeadlessLaunch.Run(arguments) : BuildAvaloniaApp().StartWithClassicDesktopLifetime(arguments);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<LauncherApp>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                RenderingMode = [Win32RenderingMode.Software],
                CompositionMode = [Win32CompositionMode.RedirectionSurface],
            });
}
