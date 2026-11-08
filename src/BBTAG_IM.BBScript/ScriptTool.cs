using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Compiling;
using BBTAG_IM.Core.Scripting.CSharp;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Scripting.EditorHelp;
using BBTAG_IM.Core.Scripting.Importing;

namespace BBTAG_IM.BBScript;

internal sealed partial class ScriptTool(ScriptDatabases databases, CommandLine commandLine)
{
    public const string Usage = """
        bbscript decompile <scr_xxx.bin | char_xxx_scr.pac>... [--out <folder>] [--python | --bbtools] [--db <bbscript folder>]
        bbscript convert <scr_xxx.py>... [--out <folder>] [--db <bbscript folder>]
        bbscript compile <scr_xxx.cs | scr_xxx.py>... [--out <folder>] [--db <bbscript folder>]
        bbscript check <scr_xxx.cs | scr_xxx.py>... [--db <bbscript folder>]
        bbscript project <folder>... [--db <bbscript folder>]
        bbscript stubs <scr_xxx.py | folder>... [--db <bbscript folder>]
        bbscript import-docs <Command Database.xlsx> [--db <bbscript folder>]
        bbscript import-bbcf <bbcf.json> [--db <bbscript folder>]
        bbscript index <char_xxx_scr.pac | scr_xxx.bin>... [--db <bbscript folder>]
        bbscript where <command | INPUT_... | MOVE_... | upon_...>... [--db <bbscript folder>]

        decompile    writes each script as C# (.cs) and checks that it builds back to the same bytes;
                     the folder also gets BBScript.g.cs, Scripts.csproj and .editorconfig for Visual Studio / VS Code
        convert      turns .py scripts into .cs scripts (comments are not carried over)
        compile      builds .cs or .py files into .bin files
        check        builds .cs or .py files without writing anything and reports every mistake with its line
        project      writes BBScript.g.cs (the API Scripts.csproj reads) into a folder
        stubs        writes __builtins__.pyi for .py scripts
        import-docs  reads the community Command Database sheet into command_docs.json for the descriptions
        import-bbcf  gives BBCF names (a BBCF command table) to the commands and SLOTs that have none yet;
                     names already there are kept and the old files are kept as .bak
        index        reads the game's scripts into script_usage.json: where every command, input and condition is used
        where        shows where the game's scripts use a command, an input, a move condition or an upon (needs index)

        --db       the folder with command_db.json (default: the bbscript folder next to this program)
        --python   write .py files (the BBTools-py3 syntax with the mod's names)
        --bbtools  write .py files with only what BBTools-py3 understands
        """;

    private const string ArchiveExtension = ".pac";
    private const string BinaryExtension = ".bin";
    private const string BackupExtension = ".bak";
    private const string EmptyDatabase = "{}";

    public bool Decompile()
    {
        var identical = true;

        foreach (var input in commandLine.Inputs)
        {
            var folder = OutputFolder(input);

            foreach (var script in ScriptsIn(input))
                identical &= DecompileOne(script, folder);
        }

        foreach (var folder in commandLine.Inputs.Select(OutputFolder).Distinct(StringComparer.OrdinalIgnoreCase))
            WriteEditorFiles(folder);

        return identical;
    }

    public bool Compile()
    {
        var built = true;

        foreach (var input in commandLine.Inputs)
        {
            if (CompileOne(input) is not { } compiled)
            {
                built = false;
                continue;
            }

            var output = Path.Combine(OutputFolder(input), Path.GetFileNameWithoutExtension(input) + BinaryExtension);
            File.WriteAllBytes(output, compiled.Bytes);
            Console.WriteLine($"{output}: {compiled.Bytes.Length} bytes");
        }

        return built;
    }

    public bool Check()
    {
        var valid = true;

        foreach (var input in commandLine.Inputs)
        {
            if (CompileOne(input) is null)
            {
                valid = false;
                continue;
            }

            Console.WriteLine($"{input}: ok");
        }

        return valid;
    }

    public bool Convert()
    {
        var converted = true;

        foreach (var input in commandLine.Inputs)
        {
            var name = Path.GetFileNameWithoutExtension(input);
            var database = databases.For(name);

            if (CompileOne(input) is not { } compiled)
            {
                converted = false;
                continue;
            }

            var folder = OutputFolder(input);
            converted &= WriteChecked(compiled.Bytes, database, name, folder, ScriptLanguage.CSharp);
            WarnAboutComments(input);
            WriteEditorFiles(folder);
        }

        return converted;
    }

    public bool Project()
    {
        foreach (var folder in commandLine.Inputs)
            WriteProject(folder);

        return true;
    }

    public bool Stubs()
    {
        foreach (var input in commandLine.Inputs)
        {
            if (Directory.Exists(input))
            {
                WriteStub(input, databases.Shared);
                continue;
            }

            WriteStub(OutputFolder(input), databases.For(Path.GetFileNameWithoutExtension(input)));
        }

        return true;
    }

    public bool ImportDocs()
    {
        foreach (var input in commandLine.Inputs)
        {
            using var stream = File.OpenRead(input);
            var docs = ScriptDocsImport.FromWorkbook(Workbook.Read(stream));

            Directory.CreateDirectory(Path.GetDirectoryName(databases.DocsPath)!);
            File.WriteAllText(databases.DocsPath, ScriptDocsFile.Write(docs));
            Console.WriteLine($"{databases.DocsPath}: {docs.Commands.Count} commands, {docs.Slots.Count} SLOTs, {docs.Upons.Count} upons");
        }

        return true;
    }

    public bool ImportBbcf()
    {
        foreach (var input in commandLine.Inputs)
        {
            var table = BbcfTable.Read(File.ReadAllText(input));
            var commands = CommandNameImport.Merge(ReadDatabase(databases.CommandsPath), table, databases.GameSizes);
            var slots = SlotNameImport.Merge(ReadDatabase(databases.GlobalSlotsPath), table);

            WriteKeepingABackup(databases.CommandsPath, commands.Json);
            WriteKeepingABackup(databases.GlobalSlotsPath, slots.Json);
            Console.WriteLine($"{databases.CommandsPath}: {commands.Named} commands named, {commands.Added} of them new");
            Console.WriteLine($"{databases.GlobalSlotsPath}: {slots.Named} SLOTs named");

            foreach (var skipped in commands.Skipped.Concat(slots.Skipped))
                Console.WriteLine("  left out: " + skipped);
        }

        return true;
    }

    private static string ReadDatabase(string path) => File.Exists(path) ? File.ReadAllText(path) : EmptyDatabase;

    private static void WriteKeepingABackup(string path, string text)
    {
        var backup = path + BackupExtension;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (File.Exists(path) && !File.Exists(backup))
            File.Copy(path, backup);

        File.WriteAllText(path, text);
    }

    public bool Index()
    {
        var usage = new ScriptUsage();
        var scripts = 0;

        foreach (var input in commandLine.Inputs)
        {
            foreach (var script in ScriptsIn(input))
            {
                var code = ScriptArchive.CharacterCode(script.Name) ?? script.Name;
                usage.Add(code, ScriptBinary.Read(script.Bytes, databases.For(script.Name)));
                scripts++;
            }
        }

        File.WriteAllText(databases.UsagePath, usage.Write());
        Console.WriteLine($"{databases.UsagePath}: {scripts} scripts, {usage.Entries.Count} commands and values");
        return true;
    }

    public bool Where()
    {
        var usage = databases.Usage();
        var found = true;

        foreach (var name in commandLine.Inputs)
            found &= WhereOne(usage, name);

        return found;
    }

    private bool WhereOne(ScriptUsage usage, string name)
    {
        if (UsageKeys.Resolve(databases.Shared, name) is not { } key)
        {
            Console.Error.WriteLine($"{name} is not a command, an input, a move condition, an upon or a hit animation.");
            return false;
        }

        if (usage.Find(key) is not { } entry)
        {
            Console.WriteLine($"{name}: not used by the indexed scripts.");
            return true;
        }

        Console.WriteLine($"{name}: {entry.Count} times, {entry.Characters.Count} characters");
        Console.WriteLine("  " + string.Join(", ", entry.Characters.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key} {pair.Value}")));

        foreach (var sample in entry.Samples)
            Console.WriteLine("  " + sample);

        return true;
    }

    private bool DecompileOne(ScriptFile script, string folder) =>
        WriteChecked(script.Bytes, databases.For(script.Name), script.Name, folder, commandLine.Language);

    private bool WriteChecked(byte[] bytes, ScriptDatabase database, string name, string folder, ScriptLanguage language)
    {
        var text = ScriptLanguages.Decompiler(database, language, commandLine.Style).Decompile(ScriptBinary.Read(bytes, database), name);
        var output = Path.Combine(folder, name + ScriptLanguages.Extension(language));

        Directory.CreateDirectory(folder);
        File.WriteAllText(output, text);

        var rebuilt = new ScriptCompiler(database, language).Compile(text).Bytes;
        var difference = rebuilt.AsSpan().CommonPrefixLength(bytes);
        var identical = rebuilt.Length == bytes.Length && difference == bytes.Length;
        Console.WriteLine(identical ? $"{output}: ok" : $"{output}: does NOT build back to the same bytes (first difference at 0x{difference:X}: {Describe(bytes, database, difference)} became {Describe(rebuilt, database, difference)})");
        return identical;
    }

    private static string Describe(byte[] bytes, ScriptDatabase database, int offset)
    {
        var script = ScriptBinary.Read(bytes, database);

        if (offset < script.DataStart)
        {
            var entry = (offset - sizeof(int)) / ScriptBinary.StateEntryLength;
            return entry >= 0 && entry < script.States.Count ? $"state table entry {entry} '{script.States[entry].Name}' at {script.States[entry].Offset}" : "the state count";
        }

        var instruction = script.Instructions.LastOrDefault(candidate => candidate.Offset <= offset);
        return instruction is null ? "nothing" : $"{instruction.Command.Name}({string.Join(", ", instruction.Values.Select(value => value.IsText ? "'" + ScriptText.Decode(value.Text) + "'" : value.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)))})";
    }

    private static void WarnAboutComments(string path)
    {
        var comments = File.ReadLines(path).Count(line => line.TrimStart().StartsWith('#') && !line.TrimStart().StartsWith(ScriptMarkers.Prefix, StringComparison.Ordinal) && !line.Contains("pyright:", StringComparison.Ordinal));

        if (comments > 0)
            Console.WriteLine($"{path}: {comments} comment lines were not carried over to the .cs file.");
    }

    private void WriteEditorFiles(string folder)
    {
        if (commandLine.Language == ScriptLanguage.CSharp)
            WriteProject(folder);
        else
            WriteStub(folder, databases.Shared);
    }

    private void WriteProject(string folder)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, CSharpApi.FileName), CSharpApi.Write(databases.Shared, databases.Docs()));
        Console.WriteLine($"{folder}: {CSharpApi.FileName} written");
    }

    private CompiledScript? CompileOne(string path)
    {
        var database = databases.For(Path.GetFileNameWithoutExtension(path));

        var source = ScriptMarkers.Strip(File.ReadAllText(path));
        var compiler = new ScriptCompiler(database, ScriptLanguages.Of(path));
        var mistakes = compiler.FindMistakes(source);

        foreach (var mistake in mistakes)
            Console.Error.WriteLine(CompilerMessages.Error(path, mistake));

        return mistakes.Count == 0 ? compiler.Compile(source) : null;
    }

    private void WriteStub(string folder, ScriptDatabase database)
    {
        var path = Path.Combine(folder, ScriptStubs.FileName);

        Directory.CreateDirectory(folder);
        File.WriteAllText(path, ScriptStubs.Write(database, databases.Docs()));
        Console.WriteLine($"{path}: written");
    }

    private static IEnumerable<ScriptFile> ScriptsIn(string path)
    {
        var bytes = File.ReadAllBytes(path);

        return Path.GetExtension(path).Equals(ArchiveExtension, StringComparison.OrdinalIgnoreCase)
            ? ScriptArchive.Scripts(bytes)
            : [new ScriptFile(Path.GetFileNameWithoutExtension(path), bytes)];
    }

    private string OutputFolder(string input) =>
        commandLine.Output ?? Path.GetDirectoryName(Path.GetFullPath(input))!;
}
