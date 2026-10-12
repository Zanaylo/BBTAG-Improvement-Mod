using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModDiscoveryTests
{
    private const string ModsDirectory = @"D:\BBTAG_IM\mods";

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    [Fact]
    public void InModsDirectory_ReturnsValidModsSortedByName()
    {
        var zeta = Path.Combine(ModsDirectory, "Zeta");
        var alpha = Path.Combine(ModsDirectory, "alpha");
        var empty = Path.Combine(ModsDirectory, "Empty");
        _fileSystem.DirectoryExists(ModsDirectory).Returns(true);
        _fileSystem.EnumerateDirectories(ModsDirectory).Returns([zeta, empty, alpha]);
        foreach (var mod in new[] { zeta, alpha, empty })
            _fileSystem.DirectoryExists(mod).Returns(true);
        _fileSystem.DirectoryExists(Path.Combine(zeta, "data")).Returns(true);
        _fileSystem.DirectoryExists(Path.Combine(alpha, "data2")).Returns(true);

        var mods = ModDiscovery.InModsDirectory(ModsDirectory, _fileSystem);

        Assert.Equal(["alpha", "Zeta"], mods.Select(mod => mod.Name));
    }

    [Fact]
    public void InModsDirectory_ReturnsEmptyWhenDirectoryIsMissing()
    {
        _fileSystem.DirectoryExists(ModsDirectory).Returns(false);

        Assert.Empty(ModDiscovery.InModsDirectory(ModsDirectory, _fileSystem));
    }
}
