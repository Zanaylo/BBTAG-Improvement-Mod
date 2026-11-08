using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.CSharp;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Scripting.EditorHelp;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Training;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Dev;

internal sealed record DevCharacter(int Slot, string Code, string Name, SpriteFolder Folder);

internal sealed class DevScripts(
    GameView game,
    CharacterRoster roster,
    IFileSystem files,
    DevFolder dev,
    ScriptFileFinder finder,
    ScriptDatabaseFolder databaseFolder,
    GameCommandTable commandTable,
    IGameHeap heap,
    IStateEntry stateEntry,
    ISubroutineCall subroutineCall,
    IMatchPause? pause,
    bool devFolderLoads,
    Func<ImprovementSettings> settings,
    ILog log)
{
    private const uint EffectsRecordOffset = ScriptRecord.Length;
    private const int FirstLine = 1;

    private static readonly string[] SlotNames = ["P1 point", "P2 point", "P1 partner", "P2 partner"];
    private static readonly bool[] ScriptKinds = [false, true];

    private readonly List<ScriptSession> _sessions = [];
    private readonly ScriptBuffers _buffers = new();
    private readonly ScriptRunners _runners = new(game);
    private readonly Dictionary<(string Code, bool Effects), IReadOnlyList<CharacterMove>> _moves = [];
    private readonly HashSet<(string Code, bool Effects)> _edited = [];
    private readonly HashSet<(uint Record, uint File)> _resumeTried = [];
    private readonly HashSet<uint> _rolledBack = [];
    private readonly ScriptFreeze? _freeze = pause is null ? null : new ScriptFreeze(pause);
    private readonly MoveRefresh _moveRefresh = new(game, new ScriptBuffers(), heap, subroutineCall);
    private CommandList _commands = commandTable.Current(game.Memory);
    private IReadOnlyDictionary<int, int>? _gameSizes;
    private ScriptDatabase? _database;

    public DevFolder Folder => dev;

    public bool HasCommandNames => databaseFolder.Exists;

    public bool DevFolderLoads => devFolderLoads;

    public bool IsFrozen => _freeze?.IsFrozen == true;

    public IReadOnlyList<ScriptSession> Sessions => _sessions;

    public static string SlotName(int slot) => SlotNames[slot];

    public IReadOnlyList<DevCharacter> Characters()
    {
        if (game.State != GameState.Match)
            return [];

        var characters = new List<DevCharacter>();

        for (var slot = 0; slot < GameView.CharacterSlots; slot++)
        {
            if (game.CharacterIndex(slot) is { } index && roster.CodeOf(index) is { } code)
                characters.Add(new DevCharacter(slot, code, roster.Identify(index).Name, roster.SpriteFolderOf(index) ?? SpriteFolder.Char));
        }

        return characters;
    }

    public string Open(DevCharacter character, bool effects)
    {
        if (RecordOf(character, effects) is not { } record)
            return "That character is not in the match anymore.";

        FollowCommandList();

        var paths = Paths(character.Code, effects);

        if (OpenSessionOf(record) is { } open)
            return OpenInEditor(open.Paths);

        try
        {
            var session = Start(character.Code, effects, record, paths);
            _sessions.Add(session);
            _edited.Add((character.Code, effects));
            SafeLog.Info(log, $"Editing {Path.GetFileName(paths.Source)} live from {paths.Source}.");
            return OpenInEditor(paths);
        }
        catch (Exception exception) when (exception is InvalidScriptException or InvalidScriptDatabaseException or InvalidSpriteDataException or IOException or UnauthorizedAccessException)
        {
            return exception.Message;
        }
    }

    public IReadOnlyList<CharacterMove> Moves(DevCharacter character, bool effects)
    {
        if (_moves.TryGetValue((character.Code, effects), out var known))
            return known;

        try
        {
            var moves = MovesOf(character.Code, effects);
            _moves[(character.Code, effects)] = moves;
            return moves;
        }
        catch (Exception exception) when (exception is InvalidScriptException or InvalidScriptDatabaseException or InvalidSpriteDataException or IOException or UnauthorizedAccessException)
        {
            SafeLog.Error(log, $"The {(effects ? "effects" : "moves")} of {character.Name} could not be read from its script: {exception.Message}");
            _moves[(character.Code, effects)] = [];
            return [];
        }
    }

    private IReadOnlyList<CharacterMove> MovesOf(string code, bool effects) =>
        finder.Find(code, effects) is { } bytes ? CharacterMoves.Read(bytes, Database(), effects) : [];

    public void OnPresent(nint device)
    {
        FollowCommandList();
        RestartRolledBackStates();
        ResumeEdits();

        foreach (var session in _sessions)
            Poll(session);

        foreach (var ended in _sessions.Where(session => session.HasEnded).ToList())
        {
            foreach (var moved in ended.Live.MovedFiles)
                _buffers.Release(moved);

            _sessions.Remove(ended);
        }

        _freeze?.Follow(_sessions.Any(session => session.WaitsForFix));
    }

    public bool Recover(string assert)
    {
        var reason = AssertMessages.Describe(assert);
        var recovered = false;

        foreach (var session in _sessions)
        {
            if (!session.Rollback(RunningInstruction(session), reason))
                continue;

            _rolledBack.Add(session.Live.RecordAddress);
            recovered = true;
        }

        return recovered || HoldForFix(reason);
    }

    private bool HoldForFix(string reason)
    {
        if (_freeze is null || !game.InTrainingMatch)
            return false;

        var held = false;

        foreach (var session in _sessions)
        {
            if (RunningInstruction(session) is { } instruction)
                held |= session.Hold(instruction, reason);
        }

        return held;
    }

    private uint? RunningInstruction(ScriptSession session) =>
        _runners.InstructionsOf(session.Live.RecordAddress).Where(address => session.Live.OffsetOf(address) is not null).Cast<uint?>().FirstOrDefault();

    private ScriptPaths Paths(string code, bool effects) => dev.Script(code, effects, settings().ScriptLanguage);

    private uint? RecordOf(DevCharacter character, bool effects) =>
        game.Character(character.Slot) is { } entity
            ? game.Memory.ReadUInt32(entity + ScriptRecord.ObjectRecordOffset) + (effects ? EffectsRecordOffset : 0)
            : null;

    private ScriptSession? OpenSessionOf(uint record) =>
        _sessions.FirstOrDefault(session => session.Live.RecordAddress == record && !session.HasEnded);

    private void RestartRolledBackStates()
    {
        foreach (var record in _rolledBack)
        {
            if (OpenSessionOf(record) is not { } session)
                continue;

            var file = new ScriptRecord(game.Memory, record).File;

            foreach (var runner in _runners.StaleOf(record, file, session.Live.Current.Bytes.Length).Where(runner => runner.State.Length > 0))
            {
                stateEntry.Enter(runner.Entity, runner.State);
                SafeLog.Info(log, $"{runner.State} was still running the script that stopped the game, so it started again on the last working version.");
            }
        }

        _rolledBack.Clear();
    }

    private void ResumeEdits()
    {
        if (_edited.Count == 0)
            return;

        foreach (var character in Characters())
        {
            foreach (var effects in ScriptKinds.Where(effects => _edited.Contains((character.Code, effects))))
                Resume(character, effects);
        }
    }

    private void Resume(DevCharacter character, bool effects)
    {
        if (RecordOf(character, effects) is not { } record || OpenSessionOf(record) is not null)
            return;

        var file = new ScriptRecord(game.Memory, record).File;

        if (file == 0 || !_resumeTried.Add((record, file)))
            return;

        var paths = Paths(character.Code, effects);

        try
        {
            _sessions.Add(Start(character.Code, effects, record, paths));
            SafeLog.Info(log, $"The match loaded again, so {Path.GetFileName(paths.Source)} is followed again and its last saved version is applied.");
        }
        catch (Exception exception) when (exception is InvalidScriptException or InvalidScriptDatabaseException or InvalidSpriteDataException or IOException or UnauthorizedAccessException)
        {
            SafeLog.Error(log, $"{Path.GetFileName(paths.Source)} could not be followed again: {exception.Message}");
        }
    }

    private string OpenInEditor(ScriptPaths paths)
    {
        var program = settings().ScriptEditor;
        var file = Path.GetFileName(paths.Source);
        var solution = ScriptLanguages.Of(paths.Source) == ScriptLanguage.CSharp ? databaseFolder.ExistingEditorSolution : null;
        ScriptEditor.Open(paths.Source, FirstLine, program, solution, log);

        return program is null
            ? $"Pick the program for {file} in the Open with window. Save the file to apply it."
            : $"Opening {file} in {Path.GetFileNameWithoutExtension(program)}. Save the file to apply it.";
    }

    private ScriptSession Start(string code, bool effects, uint recordAddress, ScriptPaths paths)
    {
        var versions = finder.Versions(code, effects);

        if (versions.Count == 0)
            throw new InvalidScriptException($"{Path.GetFileNameWithoutExtension(paths.Binary)}.bin was not found in the game data or in any mod.");

        var record = new ScriptRecord(game.Memory, recordAddress);
        var bytes = versions.FirstOrDefault(version => Runs(record, version))
            ?? throw new InvalidScriptException("The game runs a different version of this script than the files have. Load the match again.");

        var csharp = ScriptLanguages.Of(paths.Source) == ScriptLanguage.CSharp;
        var database = databaseFolder.Exists && !csharp ? databaseFolder.ForCharacter(Database(), code) : Database();
        var live = new LiveScript(game.Memory, _buffers, heap, recordAddress, ScriptBinary.Read(bytes, database), database, _moveRefresh);
        var session = new ScriptSession(files, paths, live, database);

        Directory.CreateDirectory(Path.GetDirectoryName(paths.Source)!);
        session.Open();
        WriteEditorStubs(paths, database);
        return session;
    }

    private bool Runs(ScriptRecord record, byte[] version) =>
        game.Memory.CanRead(record.File, version.Length) && game.Memory.ReadBytes(record.File, version.Length).AsSpan().SequenceEqual(version);

    private void WriteEditorStubs(ScriptPaths paths, ScriptDatabase database)
    {
        try
        {
            var docs = databaseFolder.LoadDocs();
            databaseFolder.WriteGameSizes(GameSizes());

            if (ScriptLanguages.Of(paths.Source) == ScriptLanguage.CSharp)
            {
                databaseFolder.WriteEditorApi(CSharpApi.Write(Database(), docs));
                return;
            }

            files.WriteAllText(Path.Combine(Path.GetDirectoryName(paths.Source)!, ScriptStubs.FileName), ScriptStubs.Write(database, docs));
        }
        catch (Exception exception) when (exception is InvalidScriptDatabaseException or IOException or UnauthorizedAccessException)
        {
            SafeLog.Error(log, $"The command descriptions for the editor were not written: {exception.Message}");
        }
    }

    private ScriptDatabase Database() =>
        _database ??= (databaseFolder.Exists ? databaseFolder.Load() : ScriptDatabase.Unnamed)
            .WithGameSizes(GameSizes())
            .WithTagNames();

    private IReadOnlyDictionary<int, int> GameSizes() => _gameSizes ??= _commands.ReadSizes(game.Memory);

    private void FollowCommandList()
    {
        var current = commandTable.Current(game.Memory);

        if (current == _commands)
            return;

        _commands = current;
        _gameSizes = null;
        _database = null;
        _moves.Clear();
        SafeLog.Info(log, $"The game now looks script commands up in a list of {current.Count} at 0x{current.Address:X8}, so scripts are read with that list.");
    }

    private void Poll(ScriptSession session)
    {
        var before = session.Status;

        try
        {
            session.Poll();
        }
        catch (Exception exception) when (exception is InvalidScriptException or ScriptReloadedException or PatchFailedException or IOException or UnauthorizedAccessException)
        {
            SafeLog.Error(log, $"{Path.GetFileName(session.Paths.Source)}: {exception.Message}");
        }

        if (session.Status != before)
            SafeLog.Info(log, $"{Path.GetFileName(session.Paths.Source)}: {session.Status.Message}");
    }
}
