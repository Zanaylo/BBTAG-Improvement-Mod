using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace BBTAG_IM.Launcher.Ui;

internal sealed class LauncherApp : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var launcherDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            desktop.MainWindow = new LauncherWindow(new LauncherController(desktop.Args ?? [], launcherDirectory));
        }

        base.OnFrameworkInitializationCompleted();
    }
}
