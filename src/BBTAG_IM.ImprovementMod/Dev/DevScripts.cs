using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.ImprovementMod.Frames;

namespace BBTAG_IM.ImprovementMod.Dev;

internal sealed record DevCharacter(int Slot, string Code, string Name);

internal sealed class DevScripts(
    GameView game,
    CharacterRoster roster,
    IFileSystem files,
    DevFolder dev,
    ScriptFileFinder finder,
    ScriptDatabaseFolder databaseFolder,
    IReadOnlyDictionary<int, int> gameSizes,
    Func<string?> scriptEditor,
    ILog log)
{
    private const uint EffectsRecordOffset = ScriptRecord.Length;
    private const int FirstLine = 1;

    private static readonly string[] SlotNames = ["P1 point", "P2 point", "P1 partner", "P2 partner"];

    private readonly List<ScriptSession> _sessions = [];
    private readonly ScriptBuffers _buffers = new();
    private readonly ScriptRunners _runners = new(game);
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
                characters.Add(new DevCharacter(slot, code, roster.Identify(index).Name));
        }

        return characters;
    }

    public string Open(DevCharacter character, bool effects)
    {
        if (game.Character(character.Slot) is not { } entity)
            return "That character is not in the match anymore.";

        var record = game.Memory.ReadUInt32(entity + ScriptRecord.ObjectRecordOffset) + (effects ? EffectsRecordOffset : 0);
        var paths = dev.Script(character.Code, effects);

        if (_sessions.FirstOrDefault(session => session.Live.RecordAddress == record && !session.HasEnded) is { } open)
            return OpenInEditor(open.Paths);

        try
        {
            var session = Start(character.Code, effects, record, paths);
            _sessions.Add(session);
            SafeLog.Info(log, $"Editing {Path.GetFileName(paths.Source)} live from {paths.Source}.");
            return OpenInEditor(paths);
        }
        catch (Exception exception) when (exception is InvalidScriptException or InvalidScriptDatabaseException or IOException or UnauthorizedAccessException)
        {
            return exception.Message;
        }
    }

    public void OnPresent(nint device)
    {
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

    private string OpenInEditor(ScriptPaths paths)
    {
        var program = scriptEditor();
        var file = Path.GetFileName(paths.Source);
        ScriptEditor.Open(paths.Source, FirstLine, program, log);

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

        var database = databaseFolder.Exists ? databaseFolder.ForCharacter(Database(), code) : Database();
        var live = new LiveScript(game.Memory, _buffers, recordAddress, ScriptBinary.Read(bytes, database), database);
        var session = new ScriptSession(files, paths, live, database);

        Directory.CreateDirectory(Path.GetDirectoryName(paths.Source)!);
        session.Open();
        return session;
    }

    private ScriptDatabase Database() =>
        _database ??= (databaseFolder.Exists ? databaseFolder.Load() : new ScriptDatabase([], NameTable.Empty, NameTable.Empty, NameTable.Empty, NameTable.Empty, InputNames.Empty))
            .WithGameSizes(gameSizes);

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
