using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Tests.Scripting;

public sealed class CommandDatabaseChoiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bbtag-commands-" + Guid.NewGuid().ToString("N"));

    public CommandDatabaseChoiceTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    public CommandDatabaseChoice CreateCommandDatabaseChoice() => new(new PhysicalFileSystem(), _folder);

    private string DefaultFile => Path.Combine(_folder, ScriptDatabaseFolder.CommandsFile);

    private string LibraryFile(string name)
    {
        string path = Path.Combine(_folder, CommandDatabaseChoice.LibraryFolder, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{}");
        return path;
    }

    [Fact]
    public void CurrentPath_NothingChosen_IsTheDefaultFile()
    {
        // Act
        string path = CreateCommandDatabaseChoice().CurrentPath;

        // Assert
        Assert.Equal(DefaultFile, path);
    }

    [Fact]
    public void CurrentPath_FileChosen_IsThatFile()
    {
        // Arrange
        string chosen = LibraryFile("Team Blue.json");
        CreateCommandDatabaseChoice().Choose(chosen);

        // Act
        string path = CreateCommandDatabaseChoice().CurrentPath;

        // Assert
        Assert.Equal(chosen, path);
    }

    [Fact]
    public void CurrentPath_ChosenFileWasDeleted_FallsBackToTheDefaultFile()
    {
        // Arrange
        string chosen = LibraryFile("Team Blue.json");
        CreateCommandDatabaseChoice().Choose(chosen);
        File.Delete(chosen);

        // Act
        string path = CreateCommandDatabaseChoice().CurrentPath;

        // Assert
        Assert.Equal(DefaultFile, path);
    }

    [Fact]
    public void Choose_FileInsideTheFolder_IsSavedRelativeToIt()
    {
        // Arrange
        string chosen = LibraryFile("Team Blue.json");

        // Act
        CreateCommandDatabaseChoice().Choose(chosen);

        // Assert
        Assert.Equal(Path.Combine(CommandDatabaseChoice.LibraryFolder, "Team Blue.json"), File.ReadAllText(Path.Combine(_folder, CommandDatabaseChoice.ChoiceFile)));
    }

    [Fact]
    public void Choose_TheDefaultFile_RemovesTheChoice()
    {
        // Arrange
        CommandDatabaseChoice choice = CreateCommandDatabaseChoice();
        choice.Choose(LibraryFile("Team Blue.json"));

        // Act
        choice.Choose(DefaultFile);

        // Assert
        Assert.False(File.Exists(Path.Combine(_folder, CommandDatabaseChoice.ChoiceFile)));
        Assert.Equal(DefaultFile, choice.CurrentPath);
    }

    [Fact]
    public void Available_DefaultAndLibraryFiles_ListsTheDefaultFirstThenByName()
    {
        // Arrange
        File.WriteAllText(DefaultFile, "{}");
        string tools = LibraryFile("Tools 2019.json");
        string teamBlue = LibraryFile("Team Blue.json");
        LibraryFile("notes.txt");

        // Act
        IReadOnlyList<CommandDatabaseFile> files = CreateCommandDatabaseChoice().Available();

        // Assert
        Assert.Equal([new CommandDatabaseFile("command_db.json", DefaultFile), new CommandDatabaseFile("Team Blue", teamBlue), new CommandDatabaseFile("Tools 2019", tools)], files);
    }

    [Fact]
    public void Available_ChosenFileOutsideTheLibrary_IsListedToo()
    {
        // Arrange
        string outside = Path.Combine(_folder, "elsewhere.json");
        File.WriteAllText(outside, "{}");
        CommandDatabaseChoice choice = CreateCommandDatabaseChoice();
        choice.Choose(outside);

        // Act
        IReadOnlyList<CommandDatabaseFile> files = choice.Available();

        // Assert
        Assert.Contains(new CommandDatabaseFile("elsewhere", outside), files);
    }

    [Fact]
    public void Add_FileFromElsewhere_CopiesItIntoTheLibrary()
    {
        // Arrange
        string source = Path.Combine(_folder, "CommandDB.json");
        File.WriteAllText(source, """{ "0": { "name": "startState", "format": "32s" } }""");

        // Act
        string added = CreateCommandDatabaseChoice().Add(source);

        // Assert
        Assert.Equal(Path.Combine(_folder, CommandDatabaseChoice.LibraryFolder, "CommandDB.json"), added);
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(added));
    }

    [Fact]
    public void Add_NameAlreadyInTheLibrary_KeepsBothFiles()
    {
        // Arrange
        string existing = LibraryFile("CommandDB.json");
        string source = Path.Combine(_folder, "CommandDB.json");
        File.WriteAllText(source, """{ "1": { "name": "endState", "format": "" } }""");

        // Act
        string added = CreateCommandDatabaseChoice().Add(source);

        // Assert
        Assert.Equal(Path.Combine(_folder, CommandDatabaseChoice.LibraryFolder, "CommandDB (2).json"), added);
        Assert.Equal("{}", File.ReadAllText(existing));
    }

    [Fact]
    public void Add_FileAlreadyInTheLibrary_ReturnsItWithoutCopying()
    {
        // Arrange
        string existing = LibraryFile("CommandDB.json");

        // Act
        string added = CreateCommandDatabaseChoice().Add(existing);

        // Assert
        Assert.Equal(existing, added);
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(existing)!));
    }

    [Fact]
    public void Add_FileThatIsNotACommandList_Throws()
    {
        // Arrange
        string source = Path.Combine(_folder, "broken.json");
        File.WriteAllText(source, "[ 1, 2 ]");

        // Act
        Exception? exception = Record.Exception(() => CreateCommandDatabaseChoice().Add(source));

        // Assert
        Assert.IsType<InvalidScriptDatabaseException>(exception);
    }
}
