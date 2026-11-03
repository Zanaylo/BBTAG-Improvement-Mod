using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Launch;

public class BundledFilesTests
{
    private const string LauncherFolder = @"D:\Steam\BBTAG";
    private const string FileName = "BBTAG_IM.dll";
    private const string LoosePath = @"D:\Steam\BBTAG\BBTAG_IM.dll";
    private const string CachedPath = @"D:\Steam\BBTAG\cache\BBTAG_IM.dll";

    private static readonly byte[] Bundled = [1, 2, 3];

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public BundledFiles CreateBundledFiles() => new(_fileSystemMock, LauncherFolder);

    [Fact]
    public void Place_NotCachedYet_WritesItIntoTheCacheFolder()
    {
        // Arrange
        BundledFiles files = CreateBundledFiles();

        // Act
        files.Place(FileName, Bundled);

        // Assert
        _fileSystemMock.Received(1).WriteAllBytes(CachedPath, Bundled);
    }

    [Fact]
    public void Place_NotCachedYet_ReturnsTheCachedFile()
    {
        // Arrange
        BundledFiles files = CreateBundledFiles();

        // Act
        string path = files.Place(FileName, Bundled);

        // Assert
        Assert.Equal(CachedPath, path);
    }

    [Fact]
    public void Place_SameFileCached_LeavesItAlone()
    {
        // Arrange
        _fileSystemMock.FileExists(CachedPath).Returns(true);
        _fileSystemMock.ReadAllBytes(CachedPath).Returns([1, 2, 3]);
        BundledFiles files = CreateBundledFiles();

        // Act
        files.Place(FileName, Bundled);

        // Assert
        _fileSystemMock.DidNotReceive().WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public void Place_OlderFileCached_ReplacesIt()
    {
        // Arrange
        _fileSystemMock.FileExists(CachedPath).Returns(true);
        _fileSystemMock.ReadAllBytes(CachedPath).Returns([9, 9]);
        BundledFiles files = CreateBundledFiles();

        // Act
        files.Place(FileName, Bundled);

        // Assert
        _fileSystemMock.Received(1).WriteAllBytes(CachedPath, Bundled);
    }

    [Fact]
    public void Place_OldLooseFileNextToTheLoader_StillCachesTheBundledFile()
    {
        // Arrange
        _fileSystemMock.FileExists(LoosePath).Returns(true);
        BundledFiles files = CreateBundledFiles();

        // Act
        files.Place(FileName, Bundled);

        // Assert
        _fileSystemMock.Received(1).WriteAllBytes(CachedPath, Bundled);
    }

    [Fact]
    public void PathOf_CachedFile_ReturnsItOverTheLooseFile()
    {
        // Arrange
        _fileSystemMock.FileExists(CachedPath).Returns(true);
        _fileSystemMock.FileExists(LoosePath).Returns(true);
        BundledFiles files = CreateBundledFiles();

        // Act
        string path = files.PathOf(FileName);

        // Assert
        Assert.Equal(CachedPath, path);
    }

    [Fact]
    public void PathOf_NothingCached_ReturnsTheLooseFile()
    {
        // Arrange
        BundledFiles files = CreateBundledFiles();

        // Act
        string path = files.PathOf(FileName);

        // Assert
        Assert.Equal(LoosePath, path);
    }
}
