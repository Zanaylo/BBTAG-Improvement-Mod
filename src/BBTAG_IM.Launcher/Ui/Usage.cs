namespace BBTAG_IM.Launcher.Ui;

internal static class Usage
{
    public const string Text = """
        Usage: BBTAG_Mod_Loader.exe [mod folder ...] [--mod <folder>] [--game <folder>]
        Drag one or more mod folders onto the launcher to load only those. Earlier folders win when two mods replace the same file.
        --mod or -m: same as passing the folder directly. --game or -g: the game folder, found through Steam when left out.
        Without mod folders, every folder inside "mods" next to the launcher is loaded, in name order.
        """;
}
