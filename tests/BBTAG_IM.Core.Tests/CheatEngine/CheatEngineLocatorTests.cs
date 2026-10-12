using BBTAG_IM.Core.CheatEngine;
using BBTAG_IM.Core.IO;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.CheatEngine;

public class CheatEngineLocatorTests
{
    private const string ProgramFiles = @"C:\Program Files";
    private const string ProgramFilesX86 = @"C:\Program Files (x86)";
    private const string ConfiguredFolder = @"E:\Tools\CE";
    private const string ConfiguredExecutable = @"E:\Tools\CE\cheatengine-x86_64.exe";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public CheatEngineLocatorTests()
    {
        _fileSystemMock.DirectoryExists(Arg.Any<string>()).Returns(true);
    }

    public CheatEngineLocator CreateCheatEngineLocator() => new(_fileSystemMock);

    [Fact]
    public void Locate_ConfiguredExecutableExists_ReturnsIt()
    {
        // Arrange
        CheatEngineLocator locator = CreateCheatEngineLocator();
        _fileSystemMock.FileExists(ConfiguredExecutable).Returns(true);

        // Act
        string? located = locator.Locate(ConfiguredExecutable, [ProgramFiles]);

        // Assert
        Assert.Equal(ConfiguredExecutable, located);
    }

    [Fact]
    public void Locate_ConfiguredFolderHoldsCheatEngine_ReturnsTheExecutableInside()
    {
        // Arrange
        CheatEngineLocator locator = CreateCheatEngineLocator();
        _fileSystemMock.FileExists(ConfiguredExecutable).Returns(true);

        // Act
        string? located = locator.Locate(ConfiguredFolder, [ProgramFiles]);

        // Assert
        Assert.Equal(ConfiguredExecutable, located);
    }

    [Fact]
    public void Locate_SeveralInstallsInProgramFiles_ReturnsTheNewest()
    {
        // Arrange
        CheatEngineLocator locator = CreateCheatEngineLocator();
        string old = Install(ProgramFiles, "Cheat Engine 7.5", new Version(7, 5, 0, 0));
        string current = Install(ProgramFilesX86, "Cheat Engine", new Version(7, 7, 0, 10621));
        _fileSystemMock.EnumerateDirectories(ProgramFiles).Returns([Path.Combine(ProgramFiles, "Steam"), Path.GetDirectoryName(old)!]);
        _fileSystemMock.EnumerateDirectories(ProgramFilesX86).Returns([Path.GetDirectoryName(current)!]);

        // Act
        string? located = locator.Locate(null, [ProgramFiles, ProgramFilesX86]);

        // Assert
        Assert.Equal(current, located);
    }

    [Fact]
    public void Locate_ConfiguredPathIsGone_FallsBackToProgramFiles()
    {
        // Arrange
        CheatEngineLocator locator = CreateCheatEngineLocator();
        string current = Install(ProgramFiles, "Cheat Engine", new Version(7, 7));
        _fileSystemMock.EnumerateDirectories(ProgramFiles).Returns([Path.GetDirectoryName(current)!]);

        // Act
        string? located = locator.Locate(@"E:\Gone", [ProgramFiles]);

        // Assert
        Assert.Equal(current, located);
    }

    [Fact]
    public void Locate_FolderNamedCheatEngineWithoutTheExecutable_ReturnsNull()
    {
        // Arrange
        CheatEngineLocator locator = CreateCheatEngineLocator();
        _fileSystemMock.EnumerateDirectories(ProgramFiles).Returns([Path.Combine(ProgramFiles, "Cheat Engine")]);

        // Act
        string? located = locator.Locate(null, [ProgramFiles, @"Z:\Missing"]);

        // Assert
        Assert.Null(located);
    }

    private string Install(string programFiles, string folder, Version version)
    {
        string executable = Path.Combine(programFiles, folder, CheatEngineLocator.ExecutableName);
        _fileSystemMock.FileExists(executable).Returns(true);
        _fileSystemMock.FileVersion(executable).Returns(version);
        return executable;
    }
}
