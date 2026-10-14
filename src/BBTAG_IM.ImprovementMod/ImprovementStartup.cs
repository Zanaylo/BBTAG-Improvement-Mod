using BBTAG_IM.Core.Display;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Improvement;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Online;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.ImprovementMod.Dev;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.ImprovementMod.Graphics;
using BBTAG_IM.ImprovementMod.Menu;
using BBTAG_IM.ImprovementMod.Online;
using BBTAG_IM.ImprovementMod.Palettes;
using BBTAG_IM.ImprovementMod.Training;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod;

internal static class ImprovementStartup
{
    private const string CharSelectCacheFolder = "cache";
    private const string ScriptDatabaseFolderName = "bbscript";

    public static uint Run(string modFolder, string logFolder)
    {
        var logPath = Path.Combine(logFolder, ImprovementModContract.LogFileName);

        if (File.Exists(logPath))
            File.Delete(logPath);

        var log = new FileLog(logPath, TimeProvider.System);

        try
        {
            Start(modFolder, log);
            return ModPlugin.Started;
        }
        catch (Exception exception)
        {
            log.Error(exception is HookException or PatchFailedException ? exception.Message : exception.ToString());
            return ModPlugin.Failed;
        }
    }

    private static void Start(string modFolder, ILog log)
    {
        var fileSystem = new PhysicalFileSystem();
        var settings = new SettingsWatcher<ImprovementSettings>(SettingsStores.Improvement(fileSystem, modFolder), fileSystem);
        var gameModule = LoadedModule.MainModule();
        var memory = new ProcessMemory();
        var frames = new FrameDispatcher(memory, log);
        var menuSettings = new MenuSettings(settings);
        var palettes = new PaletteLibrary(fileSystem, modFolder);
        var catalog = new PaletteCatalog(palettes, work => Task.Run(work));
        var screenPass = new BackBufferPass();
        log.Info($"Improvement Mod loaded from {modFolder}.");
        PreloadAssignedPalettes(catalog, settings.Current);

        frames.Add("Keyboard", new MenuInputInstaller(log).OnPresent);
        frames.Add("Option reloading", new SettingsRefresher(settings, log).OnPresent);
        var playerCount = new PlayerCount(new SteamPlayerCountSource(), TimeProvider.System);
        frames.Add("Online player count", _ => playerCount.Update());
        var dataFolders = GameDataFolders.Of(modFolder, fileSystem);
        log.Info($"Game data is read from {string.Join(", ", dataFolders)}.");
        var textures = new MenuTextures();
        var thumbnails = new SpriteThumbnails(new CharacterSprites(fileSystem, dataFolders, log), textures);
        AddCharSelectPalettes(gameModule, new CharSelectPalettes(fileSystem, dataFolders, catalog, () => settings.Current, Path.Combine(modFolder, CharSelectCacheFolder)), log);
        var code = gameModule.ReadCode();
        AddWindowResolutions(code, log);
        var features = AddGameFeatures(frames, gameModule, code, memory, settings, catalog, menuSettings, thumbnails, screenPass, Bosses.Installed(fileSystem, dataFolders), new DataPlaces(fileSystem, modFolder, dataFolders), log);
        IMenuSection[] sections = [features.Training, features.Palettes, new PerformanceSection(menuSettings, settings.Current.FrameLimiter), features.Dev, new ConfigSection(menuSettings)];
        AddMenu(frames, new ImprovementMenu(menuSettings, playerCount, sections, features.Windows), menuSettings, textures, screenPass, modFolder, log);
        frames.AddReset(screenPass.Release);
        AddFrameLimiter(frames, settings.Current.FrameLimiter, log);

        Direct3DHooks.Install(gameModule, settings.Current.FrameLimiter, frames);
    }

    private static GameFeatures AddGameFeatures(
        FrameDispatcher frames,
        LoadedModule gameModule,
        CodeImage code,
        ProcessMemory memory,
        SettingsWatcher<ImprovementSettings> settings,
        PaletteCatalog catalog,
        MenuSettings menuSettings,
        SpriteThumbnails thumbnails,
        BackBufferPass screenPass,
        IReadOnlyList<Boss> bosses,
        DataPlaces places,
        ILog log)
    {
        GameAddresses addresses;

        try
        {
            addresses = GameAddresses.Locate(code);
        }
        catch (GameLayoutException exception)
        {
            log.Error($"{exception.Message} Palettes and the hitbox viewer stay off.");
            var reason = $"This game version is not supported, so this stays off. {exception.Message}";
            return new GameFeatures(new UnavailableSection(TrainingSection.Name, reason), new UnavailableSection(PaletteSection.Name, reason), new UnavailableSection(DevSection.Name, reason), []);
        }

        log.Info($"Game objects found at 0x{addresses.Game:X8}, 0x{addresses.Battle:X8}, 0x{addresses.Characters:X8} and 0x{addresses.Cameras:X8}.");
        log.Info($"Palettes are read from {catalog.Library.Folder}.");

        var game = new GameView(memory, addresses);
        var roster = new CharacterRoster(memory, LocateRoster(code, log));
        var stages = new StageTable(memory, LocateStages(code, log));
        var editor = new PaletteEditor(game, roster, () => settings.Current, catalog, log, Random.Shared.Next);
        var fates = new CharacterFates(memory, roster, LocateFateFunction(code, log));
        var hitboxViewer = new HitboxViewer(game, fates, settings, new HitboxRenderer(screenPass), log);
        var choices = new PaletteChoices(editor, catalog, menuSettings);
        var editorWindow = new PaletteEditorWindow(editor, choices, menuSettings);
        var gameColors = new GameColorCount(memory, LocateColorLimit(code, log));
        var assignmentsWindow = CreateAssignmentsWindow(editor, editorWindow, choices, catalog.Library, menuSettings, thumbnails, gameColors);

        frames.Add("Palettes", _ => editor.Update());
        frames.Add("Hitbox viewer", hitboxViewer.OnPresent);
        var pause = AddPause(frames, code, game, log);
        frames.Add("Online probe", new OnlineProbe(game, memory, addresses, gameModule, log).OnPresent);
        frames.Add("Match loading", new MatchLoadLog(game, roster, stages, log).OnPresent);
        var bossFight = AddBossFight(frames, code, roster, stages, bosses, log);
        AddOwnerlessTakemiGuard(frames, code, log);
        AddSittingOutSpawnGate(frames, code, addresses.Game, log);
        var dev = AddDev(frames, gameModule, code, game, roster, places, log);

        return new GameFeatures(new TrainingSection(menuSettings, pause, bossFight), new PaletteSection(editor, choices, editorWindow, assignmentsWindow), dev, [assignmentsWindow, editorWindow]);
    }

    private static IMenuSection AddDev(FrameDispatcher frames, LoadedModule gameModule, CodeImage code, GameView game, CharacterRoster roster, DataPlaces places, ILog log)
    {
        var databaseFolder = Path.Combine(places.ModFolder, ScriptDatabaseFolderName);

        try
        {
            var table = GameCommandTable.Locate(code);
            var sizes = table.Read(game.Memory);
            var dev = DevFolder.ForMod(places.ModFolder);
            var database = new ScriptDatabaseFolder(places.Files, databaseFolder);
            var scripts = new DevScripts(game, roster, places.Files, dev, new ScriptFileFinder(places.Files, dev, places.DataFolders), database, sizes, log);
            frames.Add("Script editing", scripts.OnPresent);
            ScriptCrashGuard.Install(gameModule, scripts.Recover, log);
            log.Info($"Script editing ready: {sizes.Count} script commands read at 0x{table.Address:X8}. Command names {(database.Exists ? "come from " + databaseFolder : "are missing, so commands show as numbers")}.");
            return new DevSection(scripts, new MatchExit(game), databaseFolder);
        }
        catch (Exception exception) when (exception is GameLayoutException or HookException)
        {
            log.Error($"{exception.Message} Script editing stays off.");
            return new UnavailableSection(DevSection.Name, $"This game version is not supported, so this stays off. {exception.Message}");
        }
    }

    private static PaletteAssignmentsWindow CreateAssignmentsWindow(
        PaletteEditor editor,
        PaletteEditorWindow editorWindow,
        PaletteChoices choices,
        PaletteLibrary palettes,
        MenuSettings menuSettings,
        SpriteThumbnails thumbnails,
        GameColorCount gameColors)
    {
        var selection = new PaletteSelection(choices);
        var combo = new ColorAssignmentCombo(menuSettings, gameColors);
        var files = new PaletteFiles(palettes, menuSettings, new FileDialog(), new PaletteImport(palettes, choices, menuSettings));
        var deletion = new PaletteDeletion(palettes, choices, menuSettings, selection);
        var browser = new PaletteBrowser(choices, menuSettings, selection, combo, thumbnails);
        var details = new PaletteDetails(editor, editorWindow, selection, combo, thumbnails, files, deletion);

        return new PaletteAssignmentsWindow(menuSettings, choices, browser, details, files, deletion);
    }

    private static void PreloadAssignedPalettes(PaletteCatalog catalog, ImprovementSettings settings)
    {
        foreach (var folder in settings.Palettes.Keys)
            catalog.LoadInBackground(folder);
    }

    private static void AddCharSelectPalettes(LoadedModule gameModule, CharSelectPalettes charSelect, ILog log)
    {
        try
        {
            CharSelectFileHook.Install(gameModule, charSelect, log);
            log.Info("Assigned palettes are shown on character select.");
        }
        catch (HookException exception)
        {
            log.Error($"{exception.Message} Character select keeps the game colors.");
        }
    }

    private static void AddWindowResolutions(CodeImage code, ILog log)
    {
        try
        {
            var resolutions = WindowResolutions.Install(code);
            var choices = WindowResolutionCode.Resolutions;

            if (resolutions.AlreadyExtended)
            {
                log.Info($"The overhaul's BBTAG.exe is running. It already offers {WindowResolutionCode.ExtendedCount} window resolutions, so they stay in its order.");
                return;
            }

            log.Info($"The game's own BBTAG.exe is running. Window resolutions extended to {choices.Count} choices at 0x{resolutions.Sites.Count:X8}, 0x{resolutions.Sites.Borderless:X8} and 0x{resolutions.Sites.Window:X8}. {string.Join(", ", choices.Take(WindowResolutionCode.StockCount))} keep their places, so a resolution saved without mods means the same size here.");
        }
        catch (Exception exception) when (exception is GameLayoutException or PatchFailedException)
        {
            log.Error($"{exception.Message} The window resolutions stay as the game ships them.");
        }
    }

    private static MatchPause? AddPause(FrameDispatcher frames, CodeImage code, GameView game, ILog log)
    {
        try
        {
            var gate = PauseGate.Install(code);
            log.Info($"Pause hooked at 0x{gate.Site:X8}.");
            var pause = new MatchPause(game, gate, log);
            frames.Add("Pause", pause.OnPresent);
            return pause;
        }
        catch (Exception exception) when (exception is GameLayoutException or PatchFailedException)
        {
            log.Error($"{exception.Message} Pause and next frame stay off.");
            return null;
        }
    }

    private static BossFightControl? AddBossFight(FrameDispatcher frames, CodeImage code, CharacterRoster roster, StageTable stages, IReadOnlyList<Boss> bosses, ILog log)
    {
        try
        {
            var gate = MatchSetupGate.Install(code);
            log.Info($"Match setup hooked at 0x{gate.Site:X8} for the boss fights: {string.Join(", ", bosses.Select(boss => boss.Name))}.");
            var bossFight = new BossFightControl(gate, roster, stages, bosses, log);
            frames.Add("Boss fight", bossFight.OnPresent);
            return bossFight;
        }
        catch (Exception exception) when (exception is GameLayoutException or PatchFailedException)
        {
            log.Error($"{exception.Message} The boss fights stay off.");
            return null;
        }
    }

    private static void AddOwnerlessTakemiGuard(FrameDispatcher frames, CodeImage code, ILog log)
    {
        try
        {
            var guard = OwnerlessTakemiGuard.Install(code, log);
            log.Info($"Take-Mikazuchi body collision guarded at 0x{guard.Site:X8}.");
            frames.Add("Take-Mikazuchi body guard", guard.OnPresent);
        }
        catch (Exception exception) when (exception is GameLayoutException or PatchFailedException)
        {
            log.Error($"{exception.Message} Take-Mikazuchi's body collision is not guarded.");
        }
    }

    private static void AddSittingOutSpawnGate(FrameDispatcher frames, CodeImage code, uint game, ILog log)
    {
        try
        {
            var gate = SittingOutSpawnGate.Install(code, game, log);
            log.Info($"Characters that sit out a match cannot create objects (gate at 0x{gate.Site:X8}).");
            frames.Add("Sitting out spawn gate", gate.OnPresent);
        }
        catch (Exception exception) when (exception is GameLayoutException or PatchFailedException)
        {
            log.Error($"{exception.Message} A boss that sits out as a partner can still appear twice.");
        }
    }

    private static uint? LocateRoster(CodeImage code, ILog log)
    {
        try
        {
            var getter = CharacterRoster.LocateGetter(code);
            log.Info($"Character roster read through 0x{getter:X8}.");
            return getter;
        }
        catch (GameLayoutException exception)
        {
            log.Error($"{exception.Message} Characters the game does not ship get numbered palette folders.");
            return null;
        }
    }

    private static uint? LocateStages(CodeImage code, ILog log)
    {
        try
        {
            var getter = StageTable.LocateGetter(code);
            log.Info($"Stage list read through 0x{getter:X8}.");
            return getter;
        }
        catch (GameLayoutException exception)
        {
            log.Error($"{exception.Message} Boss fights cannot find their stage.");
            return null;
        }
    }

    private static uint? LocateFateFunction(CodeImage code, ILog log)
    {
        try
        {
            var function = FateFunction.Locate(code);
            log.Info($"Character series read through 0x{function:X8}.");
            return function;
        }
        catch (GameLayoutException exception)
        {
            log.Error($"{exception.Message} Hitboxes use the series of the characters the game ships.");
            return null;
        }
    }

    private static uint? LocateColorLimit(CodeImage code, ILog log)
    {
        try
        {
            var limit = GameColorCount.LocateLimit(code);
            log.Info($"Color count read at 0x{limit:X8}.");
            return limit;
        }
        catch (GameLayoutException exception)
        {
            log.Error($"{exception.Message} Palettes can be assigned to colors 1 to {PaletteTable.StockColors} only.");
            return null;
        }
    }

    private static void AddMenu(FrameDispatcher frames, ImprovementMenu menu, MenuSettings menuSettings, MenuTextures textures, BackBufferPass screenPass, string modFolder, ILog log)
    {
        if (Kernel32.LoadLibrary(Path.Combine(modFolder, MenuOverlay.NativeLibraryName)) == 0)
        {
            log.Error($"{MenuOverlay.NativeLibraryName} is missing from {modFolder}, so {MenuInput.ToggleKeyName} does not open the menu.");
            return;
        }

        var overlay = new MenuOverlay(menu, menuSettings, textures, screenPass, modFolder, log);
        frames.Add("Menu", overlay.OnPresent);
        frames.AddReset(overlay.OnReset);
    }

    private static void AddFrameLimiter(FrameDispatcher frames, bool turnOn, ILog log)
    {
        if (!turnOn)
        {
            log.Info("The frame limiter is off, so the game keeps its native V-Sync setting.");
            return;
        }

        var limiter = new FrameLimiter();
        frames.Add("Frame limiter", _ => limiter.WaitForNextFrame());
        log.Info(limiter.HasHighResolutionTimer
            ? $"Frame limiter on: V-Sync off, {FrameLimiter.FramesPerSecond} fps."
            : $"Frame limiter on: V-Sync off, {FrameLimiter.FramesPerSecond} fps. Windows has no high resolution timer here, so it waits by spinning.");
    }

    private sealed record GameFeatures(IMenuSection Training, IMenuSection Palettes, IMenuSection Dev, IReadOnlyList<IMenuWindow> Windows);

    private sealed record DataPlaces(IFileSystem Files, string ModFolder, IReadOnlyList<string> DataFolders);
}
