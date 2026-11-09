using BBTAG_IM.Core.Mods.Updates;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Mods.Updates;

public class ModFilePlanTests
{
    private readonly Dictionary<string, string> _previous = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _local = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _hashed = [];

    private ModFilePlan Plan(params GitTreeFile[] remote) =>
        ModFilePlan.Create(_previous, remote, _local.ContainsKey, path =>
        {
            _hashed.Add(path);
            return _local[path];
        });

    [Fact]
    public void Create_FileMissingHere_DownloadsIt()
    {
        // Arrange
        GitTreeFile file = new("data/a.pac", "s1", 1);

        // Act
        ModFilePlan plan = Plan(file);

        // Assert
        Assert.Equal([file], plan.Downloads);
    }

    [Fact]
    public void Create_FileChangedInTheRepository_DownloadsIt()
    {
        // Arrange
        _previous["data/a.pac"] = "old";
        _local["data/a.pac"] = "old";
        GitTreeFile file = new("data/a.pac", "new", 1);

        // Act
        ModFilePlan plan = Plan(file);

        // Assert
        Assert.Equal([file], plan.Downloads);
    }

    [Fact]
    public void Create_FileUnchangedInTheRepository_KeepsItWithoutReadingIt()
    {
        // Arrange
        _previous["data/a.pac"] = "same";
        _local["data/a.pac"] = "edited here";

        // Act
        ModFilePlan plan = Plan(new GitTreeFile("data/a.pac", "same", 1));

        // Assert
        Assert.Empty(plan.Downloads);
        Assert.Empty(_hashed);
    }

    [Fact]
    public void Create_FileNeverSyncedButAlreadyTheSame_KeepsIt()
    {
        // Arrange
        _local["BBTAG.exe"] = "s1";

        // Act
        ModFilePlan plan = Plan(new GitTreeFile("BBTAG.exe", "s1", 1));

        // Assert
        Assert.Empty(plan.Downloads);
    }

    [Fact]
    public void Create_FileNeverSyncedAndDifferent_DownloadsIt()
    {
        // Arrange
        _local["BBTAG.exe"] = "older";
        GitTreeFile file = new("BBTAG.exe", "s1", 1);

        // Act
        ModFilePlan plan = Plan(file);

        // Assert
        Assert.Equal([file], plan.Downloads);
    }

    [Fact]
    public void Create_FileRemovedFromTheRepository_DeletesIt()
    {
        // Arrange
        _previous["BBTAG.CT"] = "s9";
        _local["BBTAG.CT"] = "s9";

        // Act
        ModFilePlan plan = Plan();

        // Assert
        Assert.Equal(["BBTAG.CT"], plan.Deletions);
    }

    [Fact]
    public void Create_RemovedFileAlreadyGone_HasNothingToDelete()
    {
        // Arrange
        _previous["BBTAG.CT"] = "s9";

        // Act
        ModFilePlan plan = Plan();

        // Assert
        Assert.Empty(plan.Deletions);
    }

    [Fact]
    public void Create_FileThatNeverCameFromTheRepository_StaysThere()
    {
        // Arrange
        _local["data/from another mod.pac"] = "x";

        // Act
        ModFilePlan plan = Plan(new GitTreeFile("data/a.pac", "s1", 1));

        // Assert
        Assert.Empty(plan.Deletions);
    }

    [Fact]
    public void Create_PathWithOtherLetterCase_IsTheSameFile()
    {
        // Arrange
        _previous["Data/A.pac"] = "s1";
        _local["Data/A.pac"] = "s1";

        // Act
        ModFilePlan plan = Plan(new GitTreeFile("data/a.pac", "s1", 1));

        // Assert
        Assert.Empty(plan.Downloads);
        Assert.Empty(plan.Deletions);
    }
}
