using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.ImprovementMod.Menu;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod;

internal static partial class ImprovementStartup
{
    private static IReadOnlyList<string> ReadableDataFolders(string modFolder, string gameFolder, IFileSystem fileSystem) =>
        GameDataFolders.GameOnly(gameFolder);

    private static IReadOnlyList<IMenuSection> UnavailableDev(string reason) => [];

    private static EditionFeatures AddEditionFeatures(EditionContext context) => new(null, [], []);
}
