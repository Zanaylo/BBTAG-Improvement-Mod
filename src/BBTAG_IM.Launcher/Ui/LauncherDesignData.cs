namespace BBTAG_IM.Launcher.Ui;

internal static class LauncherDesignData
{
    public static LauncherViewModel ViewModel { get; } = Create();

    private static LauncherViewModel Create()
    {
        LauncherViewModel viewModel = new()
        {
            GameText = @"Game: D:\Steam\steamapps\common\BBTAG",
            ExecutableText = @"Runs: D:\Steam\steamapps\common\BBTAG\mods\BBTAG-OverHaul\BBTAG.exe",
            Mods =
            [
                new ModEntry("BBTAG-Improvement-Mod", "data folder, plugin ImprovementMod.dll", true) { CanMoveDown = true },
                new ModEntry("BBTAG-OverHaul", "data folder, BBTAG.exe, Cheat Engine table BBTAG.CT", true) { CanMoveUp = true },
            ],
            TableName = "BBTAG.CT",
            Scripts =
            [
                new ScriptNode(1, "Core / Geo Stuff", true, true, true, []),
                new ScriptNode(1567, "Danny Stuff", false, false, true,
                [
                    new ScriptNode(21610, "Training Mode Toggle Edit", true, false, false, []),
                ]),
            ],
            CanPlay = true,
        };

        viewModel.ShowActiveScripts(new HashSet<int> { 1 });
        viewModel.Report("Ready. Press Play to start the game.");
        return viewModel;
    }
}
