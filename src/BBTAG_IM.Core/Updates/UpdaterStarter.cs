using System.ComponentModel;
using System.Diagnostics;

namespace BBTAG_IM.Core.Updates;

public static class UpdaterStarter
{
    public static bool TryStartAfter(string folder, int processId)
    {
        var updater = Path.Combine(folder, ImprovementModRelease.UpdaterFileName);

        if (!File.Exists(updater))
            return false;

        var start = new ProcessStartInfo(updater) { UseShellExecute = false, WorkingDirectory = folder };

        foreach (var argument in UpdaterCommandLine.After(processId))
            start.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(start);
            return process is not null;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
