using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Improvement;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Online;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.ImprovementMod.Graphics;
using BBTAG_IM.ImprovementMod.Menu;
using BBTAG_IM.ImprovementMod.Online;
using BBTAG_IM.ImprovementMod.Training;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod;

internal static class ImprovementStartup
{
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
        log.Info($"Improvement Mod loaded from {modFolder}.");

        frames.Add("Keyboard", new MenuInputInstaller(log).OnPresent);
        frames.Add("Option reloading", new SettingsRefresher(settings, log).OnPresent);
        var playerCount = new PlayerCount(new SteamPlayerCountSource(), TimeProvider.System);
        frames.Add("Online player count", _ => playerCount.Update());
        var features = AddGameFeatures(frames, gameModule, memory, settings, palettes, menuSettings, log);
        IMenuSection[] sections = [features.Training, features.Palettes, new PerformanceSection(menuSettings, settings.Current.FrameLimiter), new ConfigSection(menuSettings)];
        AddMenu(frames, new ImprovementMenu(menuSettings, playerCount, sections, features.Windows), menuSettings, modFolder, log);
        AddFrameLimiter(frames, settings.Current.FrameLimiter, log);

        Direct3DHooks.Install(gameModule, settings.Current.FrameLimiter, frames);
    }

    private static GameFeatures AddGameFeatures(
        FrameDispatcher frames,
        LoadedModule gameModule,
        ProcessMemory memory,
        SettingsWatcher<ImprovementSettings> settings,
        PaletteLibrary palettes,
        MenuSettings menuSettings,
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
        log.Info($"Palettes are read from {palettes.Folder}.");

        var game = new GameView(memory, addresses);
        var editor = new PaletteEditor(game, new CharacterRoster(memory, LocateRoster(code, log)), () => settings.Current, palettes, log);
        var hitboxViewer = new HitboxViewer(game, settings, new HitboxRenderer(), log);
        var choices = new PaletteChoices(editor, palettes, menuSettings);
        var editorWindow = new PaletteEditorWindow(editor, choices, menuSettings);

        frames.Add("Palettes", _ => editor.Update());
        frames.Add("Hitbox viewer", hitboxViewer.OnPresent);
        var pause = AddPause(frames, code, game, log);
        frames.Add("Online probe", new OnlineProbe(game, memory, addresses, gameModule, log).OnPresent);

        return new GameFeatures(new TrainingSection(menuSettings, pause), new PaletteSection(editor, choices, editorWindow), [editorWindow]);
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

    private static void AddMenu(FrameDispatcher frames, ImprovementMenu menu, MenuSettings menuSettings, string modFolder, ILog log)
    {
        if (Kernel32.LoadLibrary(Path.Combine(modFolder, MenuOverlay.NativeLibraryName)) == 0)
        {
            log.Error($"{MenuOverlay.NativeLibraryName} is missing from {modFolder}, so {MenuInput.ToggleKeyName} does not open the menu.");
            return;
        }

        var overlay = new MenuOverlay(menu, menuSettings, log);
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
