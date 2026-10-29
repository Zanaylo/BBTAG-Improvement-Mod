using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting.Compiling;
using BBTAG_IM.Core.Scripting.Live;

namespace BBTAG_IM.Core.Scripting.Dev;

public enum ScriptSessionState
{
    Watching,
    Applied,
    CompileFailed,
    RolledBack,
    Reloaded,
}

public sealed record ScriptSessionStatus(ScriptSessionState State, string Message);

public sealed record ScriptPaths(string Source, string Binary);

public sealed class ScriptSession
{
    private readonly IFileSystem files;
    private readonly LiveScript live;
    private readonly ScriptDatabase database;
    private readonly ScriptCompiler compiler;
    private CompiledScript? previous;
    private DateTime? lastSeen;

    public ScriptSession(IFileSystem files, ScriptPaths paths, LiveScript live, ScriptDatabase database)
    {
        this.files = files;
        this.live = live;
        this.database = database;
        compiler = new ScriptCompiler(database, ScriptLanguages.Of(paths.Source));
        Paths = paths;
    }

    public ScriptPaths Paths { get; }

    public ScriptLanguage Language => ScriptLanguages.Of(Paths.Source);

    public LiveScript Live => live;

    public ScriptSessionStatus Status { get; private set; } = new(ScriptSessionState.Watching, "Save the file to apply it.");

    public bool HasEnded => Status.State == ScriptSessionState.Reloaded;

    public void Open()
    {
        if (!files.FileExists(Paths.Source))
            files.WriteAllText(Paths.Source, ScriptLanguages.Decompiler(database, Language).Decompile(live.Current, Path.GetFileNameWithoutExtension(Paths.Source)));

        lastSeen = files.LastWriteTimeUtc(Paths.Source);
    }

    public void Poll()
    {
        if (HasEnded)
            return;

        if (!live.IsLoaded)
        {
            Status = new ScriptSessionStatus(ScriptSessionState.Reloaded, "The match ended, so this script was loaded again. Open it again to keep editing.");
            return;
        }

        var saved = files.LastWriteTimeUtc(Paths.Source);

        if (saved == lastSeen)
            return;

        var previous = lastSeen;
        lastSeen = saved;

        try
        {
            Rebuild();
        }
        catch (IOException)
        {
            lastSeen = previous;
            throw;
        }
    }

    public bool Rollback(uint? instruction, string reason)
    {
        if (previous is not { } working || HasEnded)
            return false;

        var line = LineToBlame(instruction, working);
        live.Apply(working);
        files.WriteAllBytes(Paths.Binary, working.Bytes);
        previous = null;
        Mark(line ?? 1, $"The game stopped here: {reason}");
        Status = new ScriptSessionStatus(ScriptSessionState.RolledBack, $"The game stopped on this script, so the last working version is back. {reason}");
        return true;
    }

    private void Rebuild()
    {
        var text = files.ReadAllText(Paths.Source);
        var source = ScriptMarkers.Strip(text);
        CompiledScript compiled;

        try
        {
            compiled = compiler.Compile(source);
        }
        catch (ScriptCompileException exception)
        {
            MarkMistakes(source, exception);
            return;
        }

        if (source != text)
            Write(source);

        var before = new CompiledScript(live.Current.Bytes, new ScriptLineMap([]));
        var result = live.Apply(compiled);
        files.WriteAllBytes(Paths.Binary, compiled.Bytes);
        previous = before;
        Status = new ScriptSessionStatus(ScriptSessionState.Applied, Describe(result));
    }

    private void MarkMistakes(string source, ScriptCompileException first)
    {
        var mistakes = compiler.FindMistakes(source);

        if (mistakes.Count == 0)
            mistakes = [first];

        var marked = mistakes
            .OrderByDescending(mistake => mistake.Line)
            .Aggregate(source, (text, mistake) => ScriptMarkers.Insert(text, mistake.Line, mistake.Message, Language));
        var more = mistakes.Count switch
        {
            1 => string.Empty,
            2 => " (and 1 more mistake)",
            _ => $" (and {mistakes.Count - 1} more mistakes)",
        };

        Write(marked);
        Status = new ScriptSessionStatus(ScriptSessionState.CompileFailed, $"Line {mistakes[0].Line}: {mistakes[0].Message}{more}");
    }

    private int? LineToBlame(uint? instruction, CompiledScript working)
    {
        if (instruction is not { } address || live.OffsetOf(address) is not { } stopped)
            return null;

        var change = ScriptChange.Between(ScriptBinary.Read(working.Bytes, database), live.Current);
        var edited = change is InPlaceChange inPlace
            ? inPlace.Writes.Select(write => write.Offset).Where(offset => offset >= stopped).Order().Cast<int?>().FirstOrDefault()
            : null;

        return live.LineAt(edited ?? stopped);
    }

    private void Mark(int line, string message) =>
        Write(ScriptMarkers.Insert(ScriptMarkers.Strip(files.ReadAllText(Paths.Source)), line, message, Language));

    private void Write(string text)
    {
        files.WriteAllText(Paths.Source, text);
        lastSeen = files.LastWriteTimeUtc(Paths.Source);
    }

    private static string Describe(ScriptApplyResult result)
    {
        var applied = result.Kind == ScriptApplyKind.InPlace
            ? $"Applied now ({result.ChangedBytes} bytes changed)."
            : "Applied. Moves that are running finish the old way and start the new way next time.";

        return result.AddedFunctions.Count == 0
            ? applied
            : $"{applied} New states or subroutines work after the next match loads: {string.Join(", ", result.AddedFunctions)}.";
    }
}
