using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Launch;

public class ModSelectionTests
{
    private const string LauncherDirectory = @"D:\BBTAG_IM";

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    private void ModAt(string path)
    {
        _fileSystem.DirectoryExists(path).Returns(true);
        _fileSystem.DirectoryExists(Path.Combine(path, "data2")).Returns(true);
    }

    [Fact]
    public void Resolve_UsesFoldersGivenOnCommandLineInOrder()
    {
        ModAt(@"D:\Mods\Second");
        ModAt(@"D:\Mods\First");
        var options = new LaunchOptions(null, [@"D:\Mods\Second", @"D:\Mods\First"], false);

        var mods = ModSelection.Resolve(options, LauncherDirectory, _fileSystem);

        Assert.Equal(["Second", "First"], mods.Select(mod => mod.Name));
    }

    [Fact]
    public void Resolve_RejectsInvalidCommandLineFolder()
    {
        var options = new LaunchOptions(null, [@"D:\Mods\Missing"], false);

        Assert.Throws<InvalidModFolderException>(() => ModSelection.Resolve(options, LauncherDirectory, _fileSystem));
    }

    [Fact]
    public void Resolve_FallsBackToModsFolderNextToLauncher()
    {
        var modsFolder = Path.Combine(LauncherDirectory, "mods");
        var overhaul = Path.Combine(modsFolder, "Overhaul");
        _fileSystem.DirectoryExists(modsFolder).Returns(true);
        _fileSystem.EnumerateDirectories(modsFolder).Returns([overhaul]);
        ModAt(overhaul);

        var mods = ModSelection.Resolve(new LaunchOptions(null, [], false), LauncherDirectory, _fileSystem);

        Assert.Equal(["Overhaul"], mods.Select(mod => mod.Name));
    }

    [Fact]
    public void Resolve_FailsWhenThereIsNothingToLoad()
    {
        var exception = Assert.Throws<InvalidModFolderException>(
            () => ModSelection.Resolve(new LaunchOptions(null, [], false), LauncherDirectory, _fileSystem));

        Assert.Contains("mods", exception.Message);
    }
}
