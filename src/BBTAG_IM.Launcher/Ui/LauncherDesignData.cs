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
                new ModEntry("BBTAG-Improvement-Mod", "plugin ImprovementMod.dll", true),
                new ModEntry("BBTAG-OverHaul", "data folder, BBTAG.exe, Cheat Engine table BBTAG.CT", true),
            ],
            TableName = "BBTAG.CT",
            ImprovementModName = "BBTAG-Improvement-Mod",
            PalettesFolder = @"D:\Steam\steamapps\common\BBTAG\mods\BBTAG-Improvement-Mod\palettes",
            PaletteCharacters =
            [
                new CharacterPaletteChoices("Aegis", SamplePaletteColors),
                new CharacterPaletteChoices("Ragna", SamplePaletteColors),
            ],
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
        viewModel.SelectedPaletteCharacter = viewModel.PaletteCharacters[0];
        viewModel.Report("Ready. Press Play to start the game.");
        return viewModel;
    }

    private static IReadOnlyList<PaletteColorSlot> SamplePaletteColors()
    {
        PaletteOption[] options = [PaletteOption.GameColors, new("Minato", null), new("Shadow Aegis", null)];
        return Enumerable.Range(1, 20).Select(color => new PaletteColorSlot(color, options, options[color % options.Length])).ToArray();
    }
}
