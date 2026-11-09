using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Updates;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Updates;

public class InstalledCopyTests
{
    private const string Folder = @"D:\BBTAG";
    private const string Loader = @"D:\BBTAG\BBTAG_Mod_Loader.exe";
    private const string PlayerLibrary = @"D:\BBTAG\dinput8.dll";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Detect_ModLoaderThere_IsTheDevEdition()
    {
        // Arrange
        _fileSystemMock.FileExists(Loader).Returns(true);
        _fileSystemMock.FileVersion(Loader).Returns(new Version(0, 2, 0, 0));

        // Act
        InstalledCopy? copy = InstalledCopy.Detect(Folder, _fileSystemMock);

        // Assert
        Assert.Equal(new InstalledCopy(Edition.Dev, new Version(0, 2, 0, 0)), copy);
    }

    [Fact]
    public void Detect_OnlyTheLibraryThere_IsThePlayerEdition()
    {
        // Arrange
        _fileSystemMock.FileExists(PlayerLibrary).Returns(true);
        _fileSystemMock.FileVersion(PlayerLibrary).Returns(new Version(0, 2, 0, 0));

        // Act
        InstalledCopy? copy = InstalledCopy.Detect(Folder, _fileSystemMock);

        // Assert
        Assert.Equal(new InstalledCopy(Edition.Player, new Version(0, 2, 0, 0)), copy);
    }

    [Fact]
    public void Detect_NothingThere_ReturnsNull()
    {
        // Act
        InstalledCopy? copy = InstalledCopy.Detect(Folder, _fileSystemMock);

        // Assert
        Assert.Null(copy);
    }

    [Fact]
    public void Detect_FileWithoutAVersion_ReturnsNull()
    {
        // Arrange
        _fileSystemMock.FileExists(PlayerLibrary).Returns(true);
        _fileSystemMock.FileVersion(PlayerLibrary).Returns((Version?)null);

        // Act
        InstalledCopy? copy = InstalledCopy.Detect(Folder, _fileSystemMock);

        // Assert
        Assert.Null(copy);
    }
}
