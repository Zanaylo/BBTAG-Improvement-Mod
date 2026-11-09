using System.Text;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Updates;

public class GitBlobTests
{
    [Fact]
    public void Sha_EmptyFile_MatchesGit()
    {
        // Arrange
        using MemoryStream content = new();

        // Act
        string sha = GitBlob.Sha(content);

        // Assert
        Assert.Equal("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", sha);
    }

    [Fact]
    public void Sha_Text_MatchesGit()
    {
        // Arrange
        using MemoryStream content = new(Encoding.ASCII.GetBytes("hello\n"));

        // Act
        string sha = GitBlob.Sha(content);

        // Assert
        Assert.Equal("ce013625030ba8dba906f756967f9e9ca394464a", sha);
    }

    [Fact]
    public void Sha_StreamNotAtTheStart_HashesTheWholeFile()
    {
        // Arrange
        using MemoryStream content = new(Encoding.ASCII.GetBytes("hello\n"));
        content.Position = 3;

        // Act
        string sha = GitBlob.Sha(content);

        // Assert
        Assert.Equal("ce013625030ba8dba906f756967f9e9ca394464a", sha);
    }
}
