using System.Globalization;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting.Compiling;

namespace BBTAG_IM.Core.Scripting.Dev;

public sealed record DevRenameResult(int Renamed, IReadOnlyList<string> Failures, string BackupFolder);

public sealed class DevScriptRenamer(IFileSystem files, DevFolder dev)
{
    public const string BackupFolderName = ".backup";

    private const string DataFolder = "data";
    private const string BackupStampFormat = "yyyyMMdd-HHmmss";

    private static readonly string[] ScriptExtensions = [ScriptLanguages.PythonExtension, CSharp.CSharpProject.SourceExtension];

    public DevRenameResult RenameAll(
        Func<string, ScriptLanguage, ScriptDatabase> before,
        Func<string, ScriptLanguage, ScriptDatabase> after,
        DateTime now,
        ScriptStyle pythonLayout = ScriptStyle.Readable,
        ScriptLanguage? only = null)
    {
        var backupFolder = Path.Combine(dev.Path, BackupFolderName, now.ToString(BackupStampFormat, CultureInfo.InvariantCulture));
        var renamed = 0;
        var failures = new List<string>();

        foreach (var source in Scripts().Where(path => only is null || ScriptLanguages.Of(path) == only))
        {
            if (TryRename(source, before, after, backupFolder, pythonLayout) is { } failure)
                failures.Add(failure);
            else
                renamed++;
        }

        return new DevRenameResult(renamed, failures, backupFolder);
    }

    private string? TryRename(string source, Func<string, ScriptLanguage, ScriptDatabase> before, Func<string, ScriptLanguage, ScriptDatabase> after, string backupFolder, ScriptStyle pythonLayout)
    {
        var name = Path.GetFileNameWithoutExtension(source);
        var language = ScriptLanguages.Of(source);

        try
        {
            var text = files.ReadAllText(source);
            var rewritten = ScriptRenaming.Rewrite(text, language, before(name, language), after(name, language), name, pythonLayout);
            files.WriteAllText(Path.Combine(backupFolder, Path.GetRelativePath(dev.Path, source)), text);
            files.WriteAllText(source, rewritten);
            return null;
        }
        catch (Exception exception) when (exception is ScriptCompileException or InvalidScriptException or UnknownCommandException or IOException or UnauthorizedAccessException)
        {
            return $"{Path.GetFileName(source)}: {exception.Message}";
        }
    }

    private IEnumerable<string> Scripts()
    {
        var data = Path.Combine(dev.Path, DataFolder);

        if (!files.DirectoryExists(data))
            return [];

        return files.EnumerateFilesRecursively(data)
            .Where(path => ScriptExtensions.Any(extension => Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase)))
            .Where(path => ScriptArchive.CharacterCode(Path.GetFileNameWithoutExtension(path)) is not null)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
