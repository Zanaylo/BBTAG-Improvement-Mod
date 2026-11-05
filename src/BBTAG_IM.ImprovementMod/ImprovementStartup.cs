using BBTAG_IM.Core.Buttons;
using BBTAG_IM.Core.Display;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Improvement;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Online;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Roster;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.ImprovementMod.Buttons;
using BBTAG_IM.ImprovementMod.Dev;
using BBTAG_IM.ImprovementMod.Errors;
using BBTAG_IM.ImprovementMod.Files;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.ImprovementMod.Graphics;
using BBTAG_IM.ImprovementMod.Harness;
using BBTAG_IM.ImprovementMod.Menu;
using BBTAG_IM.ImprovementMod.Online;
using BBTAG_IM.ImprovementMod.Roster;
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
        var harness = HarnessServer.IsWanted ? HarnessServer.InstallEarly(gameModule, frames, log) : null;
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
        var code = gameModule.ReadCode();
        var newCharacters = AddNewCharacters(gameModule, code, memory, ModCharacterManifests.Load(EnabledMods.Of(modFolder, fileSystem), fileSystem), log);
        var cacheFolder = Path.Combine(modFolder, CharSelectCacheFolder);
        AddFileSubstitutions(
            gameModule,
            new CharSelectPalettes(fileSystem, dataFolders, catalog, () => settings.Current, cacheFolder),
            new CharacterNameFiles(fileSystem, dataFolders, [.. newCharacters.SelectMany(addition => CharacterNameEntries.For(addition.Manifest)), .. ExtraButtonTexts(settings.Current)], cacheFolder),
            settings.Current.AdvancedMapperButtons ? new ButtonIconArchive(fileSystem, dataFolders, cacheFolder) : null,
            log);
        AddWindowResolutions(gameModule, code, log);

        if (settings.Current.AdvancedMapperButtons)
            AddExtraButtons(code, log);

        var features = AddGameFeatures(frames, gameModule, code, memory, settings, catalog, menuSettings, new MenuImages(thumbnails, textures), screenPass, Bosses.Installed(fileSystem, dataFolders), new DataPlaces(fileSystem, modFolder, dataFolders), harness, log);
        IMenuSection[] sections = [features.Training, features.Palettes, new PerformanceSection(menuSettings, settings.Current.FrameLimiter), features.Dev, new ConfigSection(menuSettings)];
        AddMenu(frames, new ImprovementMenu(menuSettings, playerCount, sections, features.Windows), menuSettings, textures, screenPass, modFolder, log);
        frames.AddReset(screenPass.Release);
        AddFrameLimiter(frames, settings.Current.FrameLimiter, log);

        Direct3DHooks.Install(gameModule, settings.Current.FrameLimiter, harness is not null, frames);
    }

    private static GameFeatures AddGameFeatures(
        FrameDispatcher frames,
        LoadedModule gameModule,
        CodeImage code,
        ProcessMemory memory,
        SettingsWatcher<ImprovementSettings> settings,
        PaletteCatalog catalog,
        MenuSettings menuSettings,
        MenuImages images,
        BackBufferPass screenPass,
        IReadOnlyList<Boss> bosses,
        DataPlaces places,
        HarnessServer? harness,
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
        var matchExit = new MatchExit(game);
        harness?.Attach(game, matchExit);
        AddErrorGuard(frames, gameModule, code, game, matchExit, log);
        var roster = new CharacterRoster(memory, LocateRoster(code, log));
        var stages = new StageTable(memory, LocateStages(code, log));
        var editor = new PaletteEditor(game, roster, () => settings.Current, catalog, log, Random.Shared.Next);
        var choices = new PaletteChoices(editor, catalog, menuSettings);
        var editorWindow = new PaletteEditorWindow(editor, choices, menuSettings);
        var gameColors = new GameColorCount(memory, LocateColorLimit(code, log));
        var assignmentsWindow = CreateAssignmentsWindow(editor, editorWindow, choices, catalog.Library, menuSettings, images.Thumbnails, gameColors);

        frames.Add("Palettes", _ => editor.Update());
        var collision = AddHitboxViewer(frames, code, game, settings, screenPass, log);
        var matchPause = AddPause(frames, code, game, log);
        var pause = new PauseControls(matchPause);
        frames.Add("Online probe", new OnlineProbe(game, memory, addresses, gameModule, log).OnPresent);
        frames.Add("Match loading", new MatchLoadLog(game, roster, stages, log).OnPresent);
        var bossFight = AddBossFight(frames, code, roster, stages, bosses, log);
        AddOwnerlessTakemiGuard(frames, code, log);
        AddSittingOutSpawnGate(frames, code, addresses.Game, log);
        var preview = new SpritePreview(new CharacterImages(places.Files, places.DataFolders), images.Textures, slot => editor.Character(slot)?.Colors);
        var dev = AddDev(frames, gameModule, code, game, roster, places, menuSettings, new DevTools(matchPause, collision, preview), log);

        return new GameFeatures(new TrainingSection(menuSettings, pause, bossFight), new PaletteSection(editor, choices, editorWindow, assignmentsWindow), dev.Section, [assignmentsWindow, editorWindow, .. dev.Windows]);
    }

    private static void AddErrorGuard(FrameDispatcher frames, LoadedModule gameModule, CodeImage code, GameView game, MatchExit matchExit, ILog log)
    {
        try
        {
            FrameRecovery.Install(code, gameModule, game, matchExit, log);
            frames.Add("Error recovery", _ => FrameRecovery.OnPresent());
            log.Info("Errors during a match skip the step that failed and go back to character select instead of closing the game.");
        }
        catch (Exception exception) when (exception is GameLayoutException or PatchFailedException)
        {
            log.Error($"{exception.Message} A crash during a match still closes the game.");
        }

        try
        {
            GameErrorGuard.Install(gameModule, matchExit, log);
            log.Info("Game error boxes are written here. In a match they send it back to character select instead of closing the game.");
        }
        catch (HookException exception)
        {
            log.Error($"{exception.Message} Game error boxes close the game as usual.");
        }
    }

    private static DevFeatures AddDev(
        FrameDispatcher frames,
        LoadedModule gameModule,
        CodeImage code,
        GameView game,
        CharacterRoster roster,
        DataPlaces places,
        MenuSettings menuSettings,
        DevTools tools,
        ILog log)
    {
        var databaseFolder = Path.Combine(places.ModFolder, ScriptDatabaseFolderName);

        try
        {
            var table = GameCommandTable.Locate(code);
            var commands = table.Current(game.Memory);
            var dev = DevFolder.ForMod(places.ModFolder);
            var database = new ScriptDatabaseFolder(places.Files, databaseFolder);
            var scripts = new DevScripts(game, roster, places.Files, dev, new ScriptFileFinder(places.Files, dev, places.DataFolders), database, table, () => menuSettings.Current, log);
            frames.Add("Script editing", scripts.OnPresent);
            GameErrorGuard.RollBackScriptsWith(scripts.Recover);
            log.Info($"Script editing ready: the game looks script commands up in a list of {commands.Count} at 0x{commands.Address:X8}, read again whenever it changes. Command names {(database.Exists ? "come from " + databaseFolder : "are missing, so commands show as numbers")}.");
            var hitboxes = AddHitboxEditor(frames, game, scripts, places, dev, tools);
            var section = new DevSection(scripts, new MatchExit(game), new ScriptEditorCombo(menuSettings, new ScriptEditorList(log), new FileDialog()), new ScriptLanguageChoice(menuSettings), hitboxes, databaseFolder);
            return new DevFeatures(section, hitboxes is null ? [] : [hitboxes]);
        }
        catch (Exception exception) when (exception is GameLayoutException or HookException)
        {
            log.Error($"{exception.Message} Script editing stays off.");
            return new DevFeatures(new UnavailableSection(DevSection.Name, $"This game version is not supported, so this stays off. {exception.Message}"), []);
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

    private static IReadOnlyList<RosterAddition> AddNewCharacters(LoadedModule gameModule, CodeImage code, IGameMemory memory, ManifestLoad manifests, ILog log)
    {
        if (manifests.Manifests.Count > 0 && OverhaulExecutable.Is(gameModule.Headers.Sections))
        {
            log.Info("The overhaul's BBTAG.exe is running and its table builds the roster, so the characters of the characters folders are not added.");
            return [];
        }

        try
        {
            return NewCharacters.Install(code, memory, manifests, log);
        }
        catch (Exception exception) when (exception is GameLayoutException or PatchFailedException)
        {
            log.Error($"The new characters were not added: {exception.Message}");
            return [];
        }
    }

    private static void AddFileSubstitutions(LoadedModule gameModule, CharSelectPalettes charSelect, CharacterNameFiles names, ButtonIconArchive? icons, ILog log)
    {
        List<FileSubstitution> substitutions =
        [
            new(
                "char_select_",
                charSelect.Substitute,
                file => $"Character select reads {file} with your assigned palettes.",
                (file, error) => $"Character select keeps the game colors of {file}: {error}"),
            new(
                "_idlist.txt",
                names.Substitute,
                file => $"{file} is read with the names of the new characters.",
                (file, error) => $"{file} keeps only the game names: {error}"),
        ];

        if (icons is not null)
            substitutions.Add(new(
                ButtonIconArchive.FileName,
                icons.Substitute,
                file => $"{file} is read with the E button icon.",
                (file, error) => $"{file} keeps the game icons, so E shows no icon: {error}"));

        try
        {
            FileSubstitutionHook.Install(gameModule, substitutions, log);
            log.Info("Assigned palettes are shown on character select.");
        }
        catch (HookException exception)
        {
            log.Error($"{exception.Message} Character select keeps the game colors and the new characters have no names.");
        }
    }

    private static IReadOnlyList<KeyValuePair<string, string>> ExtraButtonTexts(ImprovementSettings settings) =>
        settings.AdvancedMapperButtons ? ExtraButtons.TextEntries : [];

    private static void AddExtraButtons(CodeImage code, ILog log)
    {
        try
        {
            ExtraButtonRows.Install(code, log);
        }
        catch (Exception exception) when (exception is GameLayoutException or PatchFailedException)
        {
            log.Error($"{exception.Message} The button settings stay as the game ships them.");
        }
    }

    private static void AddWindowResolutions(LoadedModule gameModule, CodeImage code, ILog log)
    {
        try
        {
            var resolutions = WindowResolutions.Install(gameModule, code);
            var choices = WindowResolutionCode.Resolutions;

            if (resolutions.AlreadyExtended)
            {
                log.Info($"The overhaul's BBTAG.exe is running. It keeps its {WindowResolutionCode.ExtendedCount} window resolutions in its order, and the saved resolution is matched by size, so it stays the same with the overhaul on or off.");
                return;
            }

            log.Info($"The native BBTAG.exe is running. Window resolutions extended to {choices.Count} choices at 0x{resolutions.Sites.Count:X8}, 0x{resolutions.Sites.Borderless:X8} and 0x{resolutions.Sites.Window:X8}. {string.Join(", ", choices.Take(WindowResolutionCode.StockCount))} keep their places, so a resolution saved without mods means the same size here.");
        }
        catch (Exception exception) when (exception is GameLayoutException or PatchFailedException or HookException)
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

    private static HitboxEditorWindow? AddHitboxEditor(FrameDispatcher frames, GameView game, DevScripts scripts, DataPlaces places, DevFolder dev, DevTools tools)
    {
        if (tools.Collision is not { } collision)
            return null;

        var edits = new HitboxEdits(game, dev, new ScriptBuffers(), new GameBoxReaders(game.Memory, collision.Functions.LoadBoxes), places.Files);
        var window = new HitboxEditorWindow(game, collision.Boxes, edits, scripts, tools.Match, tools.Preview, dev);
        frames.Add("Hitbox editing", window.OnPresent);
        return window;
    }

    private static LocatedCollision? AddHitboxViewer(FrameDispatcher frames, CodeImage code, GameView game, SettingsWatcher<ImprovementSettings> settings, BackBufferPass screenPass, ILog log)
    {
        try
        {
            var functions = CollisionFunctions.Locate(code);
            var collision = new GameCollision(functions);
            log.Info($"Hitboxes are read through the native collision code at 0x{functions.WorldBox:X8}.");
            frames.Add("Hitbox viewer", new HitboxViewer(game, collision, settings, new HitboxRenderer(screenPass), log).OnPresent);
            return new LocatedCollision(functions, collision);
        }
        catch (GameLayoutException exception)
        {
            log.Error($"{exception.Message} The hitbox viewer and the box editor stay off.");
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

    private sealed record LocatedCollision(CollisionFunctions Functions, GameCollision Boxes);

    private sealed record DevTools(MatchPause? Match, LocatedCollision? Collision, SpritePreview Preview);

    private sealed record MenuImages(SpriteThumbnails Thumbnails, MenuTextures Textures);

    private sealed record DevFeatures(IMenuSection Section, IReadOnlyList<IMenuWindow> Windows);
}
