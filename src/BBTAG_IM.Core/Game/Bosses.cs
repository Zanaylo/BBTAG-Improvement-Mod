using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Game;

public static class Bosses
{
    public static readonly Boss TakeMikazuchi = new("Take-Mikazuchi", "bta", "takemikazuchi", new BossSprites(SpriteFolder.Char2x, "char2x"));

    public static readonly Boss SystemXX = new("System XX", "bcr", "boss", null);

    public static readonly IReadOnlyList<Boss> All = [TakeMikazuchi, SystemXX];

    public static IReadOnlyList<Boss> Installed(IFileSystem fileSystem, IReadOnlyList<string> dataFolders) =>
        All.Where(boss => boss.IsInstalled(fileSystem, dataFolders)).ToArray();
}
