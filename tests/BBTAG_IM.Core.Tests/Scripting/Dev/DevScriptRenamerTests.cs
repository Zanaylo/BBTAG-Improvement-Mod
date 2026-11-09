using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public sealed class DevScriptRenamerTests : IDisposable
{
    private const string PythonSource = "@State\ndef A():\n    Damage(15)\n";

    private static readonly DateTime Now = new(2026, 11, 9, 18, 30, 0, DateTimeKind.Local);
    private static readonly ScriptDatabase Before = ScriptTestDatabase.Create();
    private static readonly ScriptDatabase After = new(
        Before.Commands.Select(command => command.Id == ScriptTestDatabase.Damage ? command with { Name = "SetDamage" } : command),
        Before.Slots,
        Before.Upons,
        Before.HitAnimations,
        Before.MoveConditions,
        Before.Inputs);

    private readonly string _root = Path.Combine(Path.GetTempPath(), "bbtag-rename-" + Guid.NewGuid().ToString("N"));
    private readonly DevFolder _dev;
    private readonly List<(string Name, ScriptLanguage Language)> _asked = [];

    public DevScriptRenamerTests()
    {
        _dev = new DevFolder(Path.Combine(_root, "DEV"));
        Directory.CreateDirectory(_dev.Path);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    public DevScriptRenamer CreateDevScriptRenamer() => new(new PhysicalFileSystem(), _dev);

    private DevRenameResult Rename(ScriptStyle layout = ScriptStyle.Readable, ScriptLanguage? only = null) =>
        CreateDevScriptRenamer().RenameAll(
            (name, language) =>
            {
                _asked.Add((name, language));
                return Before;
            },
            (_, _) => After,
            Now,
            layout,
            only);

    private string Script(string code, bool effects, ScriptLanguage language, string text)
    {
        string path = _dev.Script(code, effects, language).Source;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    [Fact]
    public void RenameAll_PythonScript_IsWrittenWithTheNewNames()
    {
        // Arrange
        string path = Script("brg", false, ScriptLanguage.Python, PythonSource);

        // Act
        DevRenameResult result = Rename();

        // Assert
        Assert.Contains("    SetDamage(15)", File.ReadAllText(path));
        Assert.Equal(1, result.Renamed);
    }

    [Fact]
    public void RenameAll_Script_AsksForTheDatabaseOfItsNameAndLanguage()
    {
        // Arrange
        Script("brg", true, ScriptLanguage.Python, PythonSource);

        // Act
        Rename();

        // Assert
        Assert.Equal([("scr_brgea", ScriptLanguage.Python)], _asked);
    }

    [Fact]
    public void RenameAll_Script_KeepsTheOldTextInABackup()
    {
        // Arrange
        Script("brg", false, ScriptLanguage.Python, PythonSource);

        // Act
        DevRenameResult result = Rename();

        // Assert
        string backup = Path.Combine(result.BackupFolder, "data", "char", "char_brg_scr", "scr_brg.py");
        Assert.Equal(PythonSource, File.ReadAllText(backup));
        Assert.StartsWith(Path.Combine(_dev.Path, DevScriptRenamer.BackupFolderName), result.BackupFolder);
    }

    [Fact]
    public void RenameAll_ScriptWithAMistake_IsLeftAloneAndReported()
    {
        // Arrange
        string broken = "@State\ndef A():\n    NotACommand(1)\n";
        string path = Script("brg", false, ScriptLanguage.Python, broken);

        // Act
        DevRenameResult result = Rename();

        // Assert
        Assert.Equal(broken, File.ReadAllText(path));
        Assert.Equal(0, result.Renamed);
        Assert.Single(result.Failures);
        Assert.Contains("scr_brg.py", result.Failures[0]);
    }

    [Fact]
    public void RenameAll_EditorHelpFiles_AreNotTouched()
    {
        // Arrange
        string stubs = Path.Combine(_dev.ScriptArchiveFolder("brg"), "__builtins__.pyi");
        Directory.CreateDirectory(Path.GetDirectoryName(stubs)!);
        File.WriteAllText(stubs, "def Damage(value): ...");

        // Act
        DevRenameResult result = Rename();

        // Assert
        Assert.Equal("def Damage(value): ...", File.ReadAllText(stubs));
        Assert.Equal(0, result.Renamed);
    }

    [Fact]
    public void RenameAll_OnlyPython_LeavesTheCSharpFilesAlone()
    {
        // Arrange
        string csharp = Script("brg", false, ScriptLanguage.CSharp, "untouched");
        string python = Script("brg", true, ScriptLanguage.Python, PythonSource);

        // Act
        DevRenameResult result = Rename(ScriptStyle.Mango, ScriptLanguage.Python);

        // Assert
        Assert.Equal("untouched", File.ReadAllText(csharp));
        Assert.Contains("SetDamage(15)", File.ReadAllText(python));
        Assert.Equal(1, result.Renamed);
    }

    [Fact]
    public void RenameAll_NoDevFolder_DoesNothing()
    {
        // Arrange
        Directory.Delete(_dev.Path);

        // Act
        DevRenameResult result = Rename();

        // Assert
        Assert.Equal(0, result.Renamed);
        Assert.Empty(result.Failures);
    }
}
