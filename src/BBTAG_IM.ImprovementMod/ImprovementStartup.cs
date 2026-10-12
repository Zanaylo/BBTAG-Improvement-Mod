using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Improvement;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Online;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Sprites;
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
        var features = AddGameFeatures(frames, gameModule, memory, settings, catalog, menuSettings, thumbnails, screenPass, log);
        IMenuSection[] sections = [features.Training, features.Palettes, new PerformanceSection(menuSettings, settings.Current.FrameLimiter), new ConfigSection(menuSettings)];
        AddMenu(frames, new ImprovementMenu(menuSettings, playerCount, sections, features.Windows), menuSettings, textures, screenPass, modFolder, log);
        frames.AddReset(screenPass.Release);
        AddFrameLimiter(frames, settings.Current.FrameLimiter, log);

        Direct3DHooks.Install(gameModule, settings.Current.FrameLimiter, frames);
    }

    private static GameFeatures AddGameFeatures(
        FrameDispatcher frames,
        LoadedModule gameModule,
        ProcessMemory memory,
        SettingsWatcher<ImprovementSettings> settings,
        PaletteCatalog catalog,
        MenuSettings menuSettings,
        SpriteThumbnails thumbnails,
        BackBufferPass screenPass,
        ILog log)
    {
        var code = gameModule.ReadCode();
        GameAddresses addresses;

        try
        {
            addresses = GameAddresses.Locate(code);
        }
        catch (GameLayoutException exception)
        {
            log.Error($"{exception.Message} Palettes and the hitbox viewer stay off.");
            var reason = $"This game version is not supported, so this stays off. {exception.Message}";
            return new GameFeatures(new UnavailableSection(TrainingSection.Name, reason), new UnavailableSection(PaletteSection.Name, reason), []);
        }

        log.Info($"Game objects found at 0x{addresses.Game:X8}, 0x{addresses.Battle:X8}, 0x{addresses.Characters:X8} and 0x{addresses.Cameras:X8}.");
        log.Info($"Palettes are read from {catalog.Library.Folder}.");

        var game = new GameView(memory, addresses);
        var editor = new PaletteEditor(game, new CharacterRoster(memory, LocateRoster(code, log)), () => settings.Current, catalog, log, Random.Shared.Next);
        var hitboxViewer = new HitboxViewer(game, settings, new HitboxRenderer(screenPass), log);
        var choices = new PaletteChoices(editor, catalog, menuSettings);
        var editorWindow = new PaletteEditorWindow(editor, choices, menuSettings);
        var assignmentsWindow = CreateAssignmentsWindow(editor, editorWindow, choices, catalog.Library, menuSettings, thumbnails);

        frames.Add("Palettes", _ => editor.Update());
        frames.Add("Hitbox viewer", hitboxViewer.OnPresent);
        var pause = AddPause(frames, code, game, log);
        frames.Add("Online probe", new OnlineProbe(game, memory, addresses, gameModule, log).OnPresent);

        return new GameFeatures(new TrainingSection(menuSettings, pause), new PaletteSection(editor, choices, editorWindow, assignmentsWindow), [assignmentsWindow, editorWindow]);
    }

    private static PaletteAssignmentsWindow CreateAssignmentsWindow(
        PaletteEditor editor,
        PaletteEditorWindow editorWindow,
        PaletteChoices choices,
        PaletteLibrary palettes,
        MenuSettings menuSettings,
        SpriteThumbnails thumbnails)
    {
        var selection = new PaletteSelection(choices);
        var combo = new ColorAssignmentCombo(menuSettings);
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

    private sealed record GameFeatures(IMenuSection Training, IMenuSection Palettes, IReadOnlyList<IMenuWindow> Windows);
}
