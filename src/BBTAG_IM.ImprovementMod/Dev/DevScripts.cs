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
using BBTAG_IM.ImprovementMod.Frames;

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
    Func<ImprovementSettings> settings,
    ILog log)
{
    private const uint EffectsRecordOffset = ScriptRecord.Length;
    private const int FirstLine = 1;

    private static readonly string[] SlotNames = ["P1 point", "P2 point", "P1 partner", "P2 partner"];

    private readonly List<ScriptSession> _sessions = [];
    private readonly ScriptBuffers _buffers = new();
    private readonly ScriptRunners _runners = new(game);
    private readonly Dictionary<(string Code, bool Effects), IReadOnlyList<CharacterMove>> _moves = [];
    private CommandList _commands = commandTable.Current(game.Memory);
    private IReadOnlyDictionary<int, int>? _gameSizes;
    private ScriptDatabase? _database;

    public DevFolder Folder => dev;

    public bool HasCommandNames => databaseFolder.Exists;

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
        if (game.Character(character.Slot) is not { } entity)
            return "That character is not in the match anymore.";

        FollowCommandList();

        var record = game.Memory.ReadUInt32(entity + ScriptRecord.ObjectRecordOffset) + (effects ? EffectsRecordOffset : 0);
        var paths = Paths(character.Code, effects);

        if (_sessions.FirstOrDefault(session => session.Live.RecordAddress == record && !session.HasEnded) is { } open)
            return OpenInEditor(open.Paths);

        try
        {
            var session = Start(character.Code, effects, record, paths);
            _sessions.Add(session);
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

        foreach (var session in _sessions)
            Poll(session);

        foreach (var ended in _sessions.Where(session => session.HasEnded).ToList())
        {
            foreach (var moved in ended.Live.MovedFiles)
                _buffers.Release(moved);

            _sessions.Remove(ended);
        }
    }

    public bool Recover(string assert)
    {
        var reason = AssertMessages.Describe(assert);
        var recovered = false;

        foreach (var session in _sessions)
        {
            var instruction = _runners.InstructionsOf(session.Live.RecordAddress).FirstOrDefault(address => session.Live.LineOf(address) is not null);
            recovered |= session.Rollback(instruction == 0 ? null : instruction, reason);
        }

        return recovered;
    }

    private ScriptPaths Paths(string code, bool effects) => dev.Script(code, effects, settings().ScriptLanguage);

    private string OpenInEditor(ScriptPaths paths)
    {
        var program = settings().ScriptEditor;
        var file = Path.GetFileName(paths.Source);
        var solution = ScriptLanguages.Of(paths.Source) == ScriptLanguage.CSharp ? databaseFolder.EditorSolutionPath : null;
        ScriptEditor.Open(paths.Source, FirstLine, program, solution, log);

        return program is null
            ? $"Pick the program for {file} in the Open with window. Save the file to apply it."
            : $"Opening {file} in {Path.GetFileNameWithoutExtension(program)}. Save the file to apply it.";
    }

    private ScriptSession Start(string code, bool effects, uint recordAddress, ScriptPaths paths)
    {
        var bytes = finder.Find(code, effects)
            ?? throw new InvalidScriptException($"{Path.GetFileNameWithoutExtension(paths.Binary)}.bin was not found in the game data or in any mod.");
        var record = new ScriptRecord(game.Memory, recordAddress);

        if (!game.Memory.CanRead(record.File, bytes.Length) || !game.Memory.ReadBytes(record.File, bytes.Length).AsSpan().SequenceEqual(bytes))
            throw new InvalidScriptException("The game runs a different version of this script than the files have. Load the match again.");

        var csharp = ScriptLanguages.Of(paths.Source) == ScriptLanguage.CSharp;
        var database = databaseFolder.Exists && !csharp ? databaseFolder.ForCharacter(Database(), code) : Database();
        var live = new LiveScript(game.Memory, _buffers, recordAddress, ScriptBinary.Read(bytes, database), database);
        var session = new ScriptSession(files, paths, live, database);

        Directory.CreateDirectory(Path.GetDirectoryName(paths.Source)!);
        session.Open();
        WriteEditorStubs(paths, database);
        return session;
    }

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
        catch (Exception exception) when (exception is InvalidScriptException or ScriptReloadedException or IOException or UnauthorizedAccessException)
        {
            SafeLog.Error(log, $"{Path.GetFileName(session.Paths.Source)}: {exception.Message}");
        }

        if (session.Status != before)
            SafeLog.Info(log, $"{Path.GetFileName(session.Paths.Source)}: {session.Status.Message}");
    }
}
