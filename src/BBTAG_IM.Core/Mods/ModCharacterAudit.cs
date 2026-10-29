using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting.Dev;

namespace BBTAG_IM.Core.Mods;

public sealed class ModCharacterAudit(IFileSystem files, string gameDirectory)
{
    public IReadOnlyList<CharacterFileReport> Run(IReadOnlyList<ModFolder> enabledMods, IReadOnlyList<string> rosterCodes)
    {
        var index = ModFileIndex.Build(enabledMods, files);
        var lookup = new GameFileLookup(files, gameDirectory, index, DevOverlayOf(enabledMods));
        return new CharacterFileCheck(lookup).Run(rosterCodes.Where(IsFighter));
    }

    private static bool IsFighter(string code) => Bosses.All.All(boss => boss.Code != code);

    private DevOverlay? DevOverlayOf(IEnumerable<ModFolder> enabledMods) =>
        enabledMods.FirstOrDefault(mod => mod.Name.Equals(DevFolder.Name, StringComparison.OrdinalIgnoreCase)) is { } dev
            ? new DevOverlay(files, dev.RootPath, _ => null)
            : null;
}
