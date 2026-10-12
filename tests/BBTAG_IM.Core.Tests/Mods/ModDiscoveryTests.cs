using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModDiscoveryTests
{
    private const string ModsDirectory = @"D:\BBTAG_IM\mods";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void InModsDirectory_ValidAndEmptyFolders_ReturnsTheValidModsSortedByName()
    {
        // Arrange
        string zeta = Path.Combine(ModsDirectory, "Zeta");
        string alpha = Path.Combine(ModsDirectory, "alpha");
        string empty = Path.Combine(ModsDirectory, "Empty");
        _fileSystemMock.DirectoryExists(ModsDirectory).Returns(true);
        _fileSystemMock.EnumerateDirectories(ModsDirectory).Returns([zeta, empty, alpha]);
        _fileSystemMock.DirectoryExists(Arg.Is<string>(path => path == zeta || path == alpha || path == empty)).Returns(true);
        _fileSystemMock.DirectoryExists(Path.Combine(zeta, "data")).Returns(true);
        _fileSystemMock.DirectoryExists(Path.Combine(alpha, "data2")).Returns(true);

        // Act
        IReadOnlyList<ModFolder> mods = ModDiscovery.InModsDirectory(ModsDirectory, _fileSystemMock);

        // Assert
        Assert.Equal(["alpha", "Zeta"], mods.Select(mod => mod.Name));
    }

    [Fact]
    public void InModsDirectory_MissingDirectory_ReturnsEmpty()
    {
        // Arrange
        _fileSystemMock.DirectoryExists(ModsDirectory).Returns(false);

        // Act
        IReadOnlyList<ModFolder> mods = ModDiscovery.InModsDirectory(ModsDirectory, _fileSystemMock);

        // Assert
        Assert.Empty(mods);
    }
}
