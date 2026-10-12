using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Improvement;

public static class ImprovementModContract
{
    public const string FolderName = "BBTAG-Improvement-Mod";
    public const string DllFileName = "ImprovementMod.dll";
    public const string LogFileName = "ImprovementMod.log";

    public static ModFolder? FindIn(IEnumerable<ModFolder> mods) =>
        mods.FirstOrDefault(mod => mod.PluginPaths.Any(IsImprovementDll));

    private static bool IsImprovementDll(string pluginPath) =>
        Path.GetFileName(pluginPath).Equals(DllFileName, StringComparison.OrdinalIgnoreCase);
}
