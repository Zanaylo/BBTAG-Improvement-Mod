using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.ImprovementMod.Dev;
using BBTAG_IM.ImprovementMod.Errors;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.ImprovementMod.Menu;
using BBTAG_IM.ImprovementMod.Training;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod;

internal static partial class ImprovementStartup
{
    private const string ScriptDatabaseFolderName = "bbscript";

    private static IReadOnlyList<string> ReadableDataFolders(string modFolder, string gameFolder, IFileSystem fileSystem) =>
        GameDataFolders.Of(modFolder, gameFolder, fileSystem);

    private static IReadOnlyList<IMenuSection> UnavailableDev(string reason) => [new UnavailableSection(DevSection.Name, reason)];

    private static EditionFeatures AddEditionFeatures(EditionContext context)
    {
        var bosses = Bosses.Installed(context.Places.Files, context.Places.DataFolders);
        var bossFight = AddBossFight(context.Frames, context.Code, context.Roster, context.Stages, bosses, context.Log);
        AddOwnerlessTakemiGuard(context.Frames, context.Code, context.Log);
        AddSittingOutSpawnGate(context.Frames, context.Code, context.Addresses.Game, context.Log);
        var preview = new SpritePreview(new CharacterImages(context.Places.Files, context.Places.DataFolders), context.Images.Textures, slot => context.Editor.Character(slot)?.Colors);
        var dev = AddDev(context.Frames, context.GameModule, context.Code, context.Game, context.Roster, context.Places, context.MenuSettings, new DevTools(context.Match, context.Collision, preview), context.Log);

        return new EditionFeatures(bossFight is null ? null : new BossFightPanel(bossFight), [dev.Section], dev.Windows);
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
            var heap = new GameHeap(GameHeapCode.Locate(code));
            var devFolderLoads = EnabledMods.IsOn(DevFolder.Name, places.ModFolder, places.Files);
            var finder = new ScriptFileFinder(places.Files, dev, places.DataFolders);
            var stateEntry = new GameStateEntry(StateEntryCode.Locate(code));
            var subroutineCall = new GameSubroutineCall(SubroutineCallCode.Locate(code));
            var scripts = new DevScripts(game, roster, places.Files, dev, finder, database, table, heap, stateEntry, subroutineCall, tools.Match, devFolderLoads, () => menuSettings.Current, log);
            frames.Add("Script editing", scripts.OnPresent);
            GameErrorGuard.RollBackScriptsWith(scripts.Recover);
            log.Info($"Script editing ready: the game looks script commands up in a list of {commands.Count} at 0x{commands.Address:X8}, read again whenever it changes. Command names {(database.Exists ? "come from " + databaseFolder : "are missing, so commands show as numbers")}.");
            var hitboxes = AddHitboxEditor(frames, game, scripts, places, dev, tools);
            var help = new ScriptHelpWindow();
            var section = new DevSection(scripts, new MatchExit(game), new ScriptEditorCombo(menuSettings, new ScriptEditorList(log), new FileDialog()), new ScriptLanguageChoice(menuSettings, scripts), new CommandNamesCombo(scripts, new FileDialog()), help, hitboxes, databaseFolder);
            return new DevFeatures(section, hitboxes is null ? [help] : [hitboxes, help]);
        }
        catch (Exception exception) when (exception is GameLayoutException or HookException)
        {
            log.Error($"{exception.Message} Script editing stays off.");
            return new DevFeatures(new UnavailableSection(DevSection.Name, $"This game version is not supported, so this stays off. {exception.Message}"), []);
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

    private static HitboxEditorWindow? AddHitboxEditor(FrameDispatcher frames, GameView game, DevScripts scripts, DataPlaces places, DevFolder dev, DevTools tools)
    {
        if (tools.Collision is not { } collision)
            return null;

        var edits = new HitboxEdits(game, dev, new ScriptBuffers(), new GameBoxReaders(game.Memory, collision.Functions.LoadBoxes), places.Files);
        var window = new HitboxEditorWindow(game, collision.Boxes, edits, scripts, tools.Match, tools.Preview, dev);
        frames.Add("Hitbox editing", window.OnPresent);
        return window;
    }

    private sealed record DevTools(MatchPause? Match, LocatedCollision? Collision, SpritePreview Preview);

    private sealed record DevFeatures(IMenuSection Section, IReadOnlyList<IMenuWindow> Windows);
}
