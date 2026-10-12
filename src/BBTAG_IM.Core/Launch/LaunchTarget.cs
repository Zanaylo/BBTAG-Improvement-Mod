using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Pe;

namespace BBTAG_IM.Core.Launch;

public sealed record LaunchTarget(string ExecutablePath, ModFolder? ExecutableMod)
{
    public static LaunchTarget Choose(string gameDirectory, IReadOnlyList<ModFolder> mods, Func<string, PeHeaders> readHeaders)
    {
        var gameExecutable = new LaunchTarget(Path.Combine(gameDirectory, ModFolder.GameExecutableName), null);
        var executableMod = mods.FirstOrDefault(mod => mod.ExecutablePath is not null);

        if (executableMod is null)
            return gameExecutable;

        return readHeaders(executableMod.ExecutablePath!).RelocationsStripped
            ? new LaunchTarget(executableMod.ExecutablePath!, executableMod)
            : gameExecutable;
    }

    public static bool IsInstalledGame(string executablePath, string gameDirectory) =>
        Path.GetFullPath(executablePath).Equals(
            Path.GetFullPath(Path.Combine(gameDirectory, ModFolder.GameExecutableName)), StringComparison.OrdinalIgnoreCase);
}
