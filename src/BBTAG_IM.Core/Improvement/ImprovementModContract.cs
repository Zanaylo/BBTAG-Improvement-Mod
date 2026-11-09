using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Improvement;

public static class ImprovementModContract
{
    public const string LogFileName = "ImprovementMod.log";
    public const string GameFolderVariable = "BBTAG_IM_GAME_FOLDER";
    public const string ModFolderName = "BBTAG-Improvement-Mod";

    public static string GameFolder(string? announcedFolder, string modFolder) =>
        string.IsNullOrEmpty(announcedFolder) ? EnabledMods.GameFolderOf(modFolder) : announcedFolder;
}
