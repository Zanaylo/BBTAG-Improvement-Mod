using BBTAG_IM.Core.Mods.Updates;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Mods.Updates;

public class RepositoryLayoutTests
{
    private static GitTreeFile File(string path) => new(path, "sha-" + path, 1);

    [Fact]
    public void ModFiles_ModAtTheTop_KeepsThePaths()
    {
        // Arrange
        GitTreeFile[] tree = [File("BBTAG.exe"), File("data/char/a.pac")];

        // Act
        IReadOnlyList<GitTreeFile> files = RepositoryLayout.ModFiles(tree);

        // Assert
        Assert.Equal(tree, files);
    }

    [Theory]
    [InlineData("Table.CT")]
    [InlineData("Plugin.dll")]
    [InlineData("data2/a.pac")]
    public void ModFiles_AnyModFileAtTheTop_FindsTheModThere(string path)
    {
        // Arrange
        GitTreeFile[] tree = [File(path)];

        // Act
        IReadOnlyList<GitTreeFile> files = RepositoryLayout.ModFiles(tree);

        // Assert
        Assert.Equal(tree, files);
    }

    [Fact]
    public void ModFiles_ModInsideOneFolder_StripsTheFolder()
    {
        // Arrange
        GitTreeFile[] tree = [File("README.md"), File("Mod/data/a.pac"), File("Mod/Notes.txt")];

        // Act
        IReadOnlyList<GitTreeFile> files = RepositoryLayout.ModFiles(tree);

        // Assert
        Assert.Equal(["data/a.pac", "Notes.txt"], files.Select(file => file.Path));
        Assert.Equal("sha-Mod/data/a.pac", files[0].Sha);
    }

    [Fact]
    public void ModFiles_GitFiles_AreLeftOut()
    {
        // Arrange
        GitTreeFile[] tree = [File(".gitattributes"), File(".github/workflows/build.yml"), File("data/.gitkeep"), File("data/a.pac")];

        // Act
        IReadOnlyList<GitTreeFile> files = RepositoryLayout.ModFiles(tree);

        // Assert
        Assert.Equal(["data/a.pac"], files.Select(file => file.Path));
    }

    [Fact]
    public void ModFiles_NothingToLoad_Throws()
    {
        // Arrange
        GitTreeFile[] tree = [File("README.md"), File("docs/a.md"), File("src/a.cs")];

        // Act
        Exception? exception = Record.Exception(() => RepositoryLayout.ModFiles(tree));

        // Assert
        Assert.IsType<ModUpdateException>(exception);
    }
}
