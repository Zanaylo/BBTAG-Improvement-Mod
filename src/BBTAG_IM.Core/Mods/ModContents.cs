namespace BBTAG_IM.Core.Mods;

public static class ModContents
{
    private const string Separator = ", ";
    private const string NothingToLoad = "nothing to load yet";

    public static string Describe(ModFolder mod, Func<string, bool> isPlugin) =>
        mod.HasGameFiles ? string.Join(Separator, PartsOf(mod, isPlugin)) : NothingToLoad;

    private static IEnumerable<string> PartsOf(ModFolder mod, Func<string, bool> isPlugin) =>
        mod.DataRoots.Select(root => $"{Path.GetFileName(root.Path)} folder")
            .Concat(mod.EncryptedAssetsPath is null ? [] : [$"{Path.GetFileName(mod.EncryptedAssetsPath)} folder (encrypted)"])
            .Concat(mod.ExecutablePath is null ? [] : [ModFolder.GameExecutableName])
            .Concat(mod.TablePath is null ? [] : [$"Cheat Engine table {Path.GetFileName(mod.TablePath)}"])
            .Concat(mod.PluginPaths.Select(dll => $"{(isPlugin(dll) ? "plugin" : "library")} {Path.GetFileName(dll)}"));
}
