using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.EditorHelp;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Scripting;

public class ScriptDatabaseFolderTests
{
    private const string Folder = @"C:\Game\mods\BBTAG-Improvement-Mod\bbscript";
    private const string EditorApi = @"C:\Game\mods\BBTAG-Improvement-Mod\bbscript\Scripts\BBScript.g.cs";
    private static readonly string DocsPath = Path.Combine(Folder, ScriptDocsFile.FileName);
    private static readonly string GameSizesPath = Path.Combine(Folder, ScriptDatabaseFolder.GameSizesFile);

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void LoadDocs_DocsFileExists_ReadsIt()
    {
        // Arrange
        _fileSystemMock.FileExists(DocsPath).Returns(true);
        _fileSystemMock.ReadAllText(DocsPath).Returns("""{ "commands": { "9000": { "summary": "Sets the damage." } } }""");
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        ScriptDocs docs = folder.LoadDocs();

        // Assert
        Assert.Equal("Sets the damage.", docs.Command(9000)?.Summary);
    }

    [Fact]
    public void LoadDocs_NoDocsFile_ReturnsEmptyDocs()
    {
        // Arrange
        _fileSystemMock.FileExists(DocsPath).Returns(false);
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        ScriptDocs docs = folder.LoadDocs();

        // Assert
        Assert.Same(ScriptDocs.Empty, docs);
    }

    [Fact]
    public void LoadUsage_UsageFileExists_ReadsIt()
    {
        // Arrange
        string path = Path.Combine(Folder, ScriptUsage.FileName);
        _fileSystemMock.FileExists(path).Returns(true);
        _fileSystemMock.ReadAllText(path).Returns("""{ "entries": { "Command:9000": { "count": 3, "characters": { "brg": 3 }, "samples": [ "brg NmlAtk5A" ] } } }""");
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        ScriptUsage usage = folder.LoadUsage();

        // Assert
        Assert.Equal(3, usage.Find(new UsageKey(UsageKind.Command, 9000))?.Count);
    }

    [Fact]
    public void LoadUsage_NoUsageFile_ReturnsEmptyUsage()
    {
        // Arrange
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        ScriptUsage usage = folder.LoadUsage();

        // Assert
        Assert.Same(ScriptUsage.Empty, usage);
    }

    [Fact]
    public void DocsPath_IsInsideTheFolder()
    {
        // Arrange
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        string path = folder.DocsPath;

        // Assert
        Assert.Equal(DocsPath, path);
    }

    [Fact]
    public void EditorSolutionPath_IsTheBBScriptSolutionInsideTheScriptsFolder()
    {
        // Arrange
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        string path = folder.EditorSolutionPath;

        // Assert
        Assert.Equal(@"C:\Game\mods\BBTAG-Improvement-Mod\bbscript\Scripts\BBScript.slnx", path);
    }

    [Fact]
    public void LoadGameSizes_NoSizesFile_ReturnsNull()
    {
        // Arrange
        _fileSystemMock.FileExists(GameSizesPath).Returns(false);
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        IReadOnlyDictionary<int, int>? sizes = folder.LoadGameSizes();

        // Assert
        Assert.Null(sizes);
    }

    [Fact]
    public void LoadGameSizes_SizesFileExists_ReadsIt()
    {
        // Arrange
        _fileSystemMock.FileExists(GameSizesPath).Returns(true);
        _fileSystemMock.ReadAllText(GameSizesPath).Returns("0\t36\n40019\t8\n");
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        IReadOnlyDictionary<int, int>? sizes = folder.LoadGameSizes();

        // Assert
        Assert.Equal(new Dictionary<int, int> { [0] = 36, [40019] = 8 }, sizes);
    }

    [Fact]
    public void WriteGameSizes_SizesChanged_WritesThem()
    {
        // Arrange
        _fileSystemMock.FileExists(GameSizesPath).Returns(true);
        _fileSystemMock.ReadAllText(GameSizesPath).Returns("0\t36\n");
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        folder.WriteGameSizes(new Dictionary<int, int> { [0] = 36, [40019] = 8 });

        // Assert
        _fileSystemMock.Received(1).WriteAllText(GameSizesPath, "0\t36\n40019\t8\n");
    }

    [Fact]
    public void WriteGameSizes_SameSizes_LeavesTheFileAlone()
    {
        // Arrange
        _fileSystemMock.FileExists(GameSizesPath).Returns(true);
        _fileSystemMock.ReadAllText(GameSizesPath).Returns("0\t36\n");
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        folder.WriteGameSizes(new Dictionary<int, int> { [0] = 36 });

        // Assert
        _fileSystemMock.DidNotReceive().WriteAllText(GameSizesPath, Arg.Any<string>());
    }

    [Fact]
    public void WriteEditorApi_NoApiYet_WritesIt()
    {
        // Arrange
        _fileSystemMock.FileExists(EditorApi).Returns(false);
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        folder.WriteEditorApi("api");

        // Assert
        _fileSystemMock.Received(1).WriteAllText(EditorApi, "api");
    }

    [Fact]
    public void WriteEditorApi_ApiChanged_WritesIt()
    {
        // Arrange
        _fileSystemMock.FileExists(EditorApi).Returns(true);
        _fileSystemMock.ReadAllText(EditorApi).Returns("old api");
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        folder.WriteEditorApi("api");

        // Assert
        _fileSystemMock.Received(1).WriteAllText(EditorApi, "api");
    }

    [Fact]
    public void WriteEditorApi_SameApi_LeavesTheFileAlone()
    {
        // Arrange
        _fileSystemMock.FileExists(EditorApi).Returns(true);
        _fileSystemMock.ReadAllText(EditorApi).Returns("api");
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        folder.WriteEditorApi("api");

        // Assert
        _fileSystemMock.DidNotReceive().WriteAllText(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void CommandsPath_Folder_IsTheCommandFileInIt()
    {
        // Arrange
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        string path = folder.CommandsPath;

        // Assert
        Assert.Equal(Path.Combine(Folder, "command_db.json"), path);
    }

    [Fact]
    public void GlobalSlotsPath_Folder_IsTheGlobalFileOfTheSlotFolder()
    {
        // Arrange
        ScriptDatabaseFolder folder = CreateScriptDatabaseFolder();

        // Act
        string path = folder.GlobalSlotsPath;

        // Assert
        Assert.Equal(Path.Combine(Folder, "slot_db", "global.json"), path);
    }

    public ScriptDatabaseFolder CreateScriptDatabaseFolder() => new(_fileSystemMock, Folder);
}
