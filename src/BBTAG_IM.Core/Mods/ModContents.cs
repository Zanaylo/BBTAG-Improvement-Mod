namespace BBTAG_IM.Core.Mods;

public static class ModContents
{
    private const string Separator = ", ";

    public static string Describe(ModFolder mod, Func<string, bool> isPlugin) =>
        string.Join(Separator, mod.DataRoots.Select(root => $"{Path.GetFileName(root.Path)} folder")
            .Concat(mod.ExecutablePath is null ? [] : [ModFolder.GameExecutableName])
            .Concat(mod.TablePath is null ? [] : [$"Cheat Engine table {Path.GetFileName(mod.TablePath)}"])
            .Concat(mod.PluginPaths.Select(dll => $"{(isPlugin(dll) ? "plugin" : "library")} {Path.GetFileName(dll)}")));
}
