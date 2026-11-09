using System.Diagnostics;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Updates;
using BBTAG_IM.Updater.Dialogs;
using BBTAG_IM.Updater.Native;

namespace BBTAG_IM.Updater;

internal sealed class UpdaterApp(string folder, UpdaterCommandLine commandLine, IGitHubApi gitHub, IFileSystem files)
{
    private const int UpdateNow = 100;
    private const int NotNow = 101;
    private const int SkipVersion = 102;
    private const double Megabyte = 1024 * 1024;

    private readonly ReleaseInstaller _installer = new();
    private InstallWork? _work;

    public void Run()
    {
        _installer.RemoveLeftovers(folder);
        var installed = InstalledCopy.Detect(folder, files);

        if (commandLine.AfterProcess is { } process)
            RunAfter(process, installed);
        else
            RunByHand(installed);
    }

    private void RunAfter(int process, InstalledCopy? installed)
    {
        if (installed is null || TryFindOffer(installed) is not { } offer || LoadSettings().Skips(offer.Version))
            return;

        WaitForExit(process);
        Show(OfferPage(installed, offer));
    }

    private void RunByHand(InstalledCopy? installed)
    {
        if (installed is null)
        {
            Show(MessagePage("The Improvement Mod is not in this folder", $"Put {ImprovementModRelease.UpdaterFileName} next to BBTAG.exe, in the folder that has {InstalledCopy.PlayerLibraryFileName} or {InstalledCopy.ModLoaderFileName}."));
            return;
        }

        Show(CheckingPage(installed));
    }

    private void Show(TaskDialogPage page)
    {
        using var dialog = new TaskDialogRunner();
        dialog.Show(page);
        _work?.WaitUntilStopped();
    }

    private UpdateOffer? TryFindOffer(InstalledCopy installed)
    {
        try
        {
            return FindOfferAsync(installed).GetAwaiter().GetResult();
        }
        catch (GitHubException)
        {
            return null;
        }
    }

    private async Task<UpdateOffer?> FindOfferAsync(InstalledCopy installed)
    {
        var release = await gitHub.LatestReleaseAsync(ImprovementModRelease.Repository, CancellationToken.None);
        return release is null ? null : ImprovementModRelease.OfferFor(release, installed);
    }

    private TaskDialogPage CheckingPage(InstalledCopy installed)
    {
        var check = Task.Run(() => FindOfferAsync(installed));

        return new TaskDialogPage
        {
            Instruction = "Looking for a new version",
            Content = $"You have version {ReleaseVersion.Text(installed.Version)}.",
            Progress = PageProgress.Marquee,
            CommonButtons = TaskDialogNative.CancelButton,
            Tick = dialog =>
            {
                if (!check.IsCompleted)
                    return;

                dialog.Navigate(check.Exception?.InnerException is { } failure
                    ? MessagePage("The check did not finish", failure.Message)
                    : check.Result is { } offer ? OfferPage(installed, offer) : MessagePage("You have the newest version", $"Version {ReleaseVersion.Text(installed.Version)} is the newest one."));
            },
        };
    }

    private TaskDialogPage OfferPage(InstalledCopy installed, UpdateOffer offer) => new()
    {
        Instruction = $"Version {ReleaseVersion.Text(offer.Version)} is out",
        Content = $"You have version {ReleaseVersion.Text(installed.Version)}. The update replaces the Improvement Mod files in this folder. Your options, palettes and mods stay.\n\n<a href=\"{offer.Page}\">See what changed</a>",
        Buttons =
        [
            new DialogButton(UpdateNow, $"Update now\nDownloads {offer.Asset.Size / Megabyte:0.0} MB from GitHub."),
            new DialogButton(NotNow, "Not now\nAsks again the next time the game starts."),
            new DialogButton(SkipVersion, $"Skip version {ReleaseVersion.Text(offer.Version)}\nAsks again when a newer one comes out."),
        ],
        LinkClicked = OpenInBrowser,
        ButtonClicked = (dialog, button) => OnOfferChosen(dialog, button, installed, offer),
    };

    private bool OnOfferChosen(TaskDialogRunner dialog, int button, InstalledCopy installed, UpdateOffer offer)
    {
        if (button == SkipVersion)
            SaveSkipped(offer.Version);

        if (button != UpdateNow)
            return false;

        dialog.Navigate(InstallPage(installed, offer));
        return true;
    }

    private TaskDialogPage InstallPage(InstalledCopy installed, UpdateOffer offer)
    {
        var work = new InstallWork(gitHub, _installer, folder, offer);
        _work = work;

        return new TaskDialogPage
        {
            Instruction = $"Updating to version {ReleaseVersion.Text(offer.Version)}",
            Content = "Starting the download.",
            Progress = PageProgress.Bar,
            CommonButtons = TaskDialogNative.CancelButton,
            Opened = _ => work.Start(),
            Tick = dialog => OnInstallTick(dialog, work, installed, offer),
            ButtonClicked = (_, _) =>
            {
                work.Cancel();
                return work.IsInstalling;
            },
        };
    }

    private static void OnInstallTick(TaskDialogRunner dialog, InstallWork work, InstalledCopy installed, UpdateOffer offer)
    {
        if (work.IsFinished)
        {
            dialog.Navigate(work.Failure is { } failure ? MessagePage("The update did not finish", failure) : DonePage(installed, offer));
            return;
        }

        if (work.IsInstalling)
        {
            dialog.EnableButton(TaskDialogNative.CancelId, false);
            dialog.SetProgress(1);
            dialog.SetContent("Replacing the files.");
            return;
        }

        dialog.SetProgress(work.Size == 0 ? 0 : (double)work.Downloaded / work.Size);
        dialog.SetContent($"Downloading {work.Downloaded / Megabyte:0.0} of {work.Size / Megabyte:0.0} MB.");
    }

    private static TaskDialogPage DonePage(InstalledCopy installed, UpdateOffer offer) => new()
    {
        Instruction = $"Updated to version {ReleaseVersion.Text(offer.Version)}",
        Content = installed.Edition == Edition.Dev ? $"Open {InstalledCopy.ModLoaderFileName} to play." : "Start the game as usual.",
        CommonButtons = TaskDialogNative.CloseButton,
    };

    private static TaskDialogPage MessagePage(string instruction, string content) => new()
    {
        Instruction = instruction,
        Content = content,
        CommonButtons = TaskDialogNative.CloseButton,
    };

    private UpdaterSettings LoadSettings()
    {
        try
        {
            return SettingsStores.Updater(files, folder).Load();
        }
        catch (InvalidSettingsFileException)
        {
            return new UpdaterSettings();
        }
    }

    private void SaveSkipped(Version version)
    {
        var settings = LoadSettings();
        settings.Skip(version);

        try
        {
            SettingsStores.Updater(files, folder).Save(settings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }
    }

    private static void WaitForExit(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.WaitForExit();
        }
        catch (ArgumentException)
        {
            return;
        }
    }

    private static void OpenInBrowser(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return;

        using var browser = Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}
