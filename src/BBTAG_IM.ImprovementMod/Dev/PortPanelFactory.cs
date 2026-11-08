using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.ImprovementMod.Menu;

namespace BBTAG_IM.ImprovementMod.Dev;

internal static class PortPanelFactory
{
    private const string ScriptToolName = "bbscript.exe";

    public static IDevPanel Create(IFileSystem files, string modFolder, string databaseFolder, ILog log)
    {
        var gameData = Path.Combine(EnabledMods.GameFolderOf(modFolder), ModFolder.GameDataFolder);
        var mods = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(modFolder)) ?? modFolder;
        var runner = new PortRunner(Path.Combine(modFolder, ScriptToolName), log);
        return new CharacterPortPanel(files, new FileDialog(), runner, new PortFolders(gameData, mods, databaseFolder));
    }
}
