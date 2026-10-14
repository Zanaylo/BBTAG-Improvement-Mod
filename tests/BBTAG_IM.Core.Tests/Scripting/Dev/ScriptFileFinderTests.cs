using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class ScriptFileFinderTests
{
    private const string ModFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod";
    private const string GameData = @"D:\BBTAG\data";
    private const string Archive = @"D:\BBTAG\data\char\char_brg_scr.pac";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Find_ScriptOnlyInTheGameArchive_ReturnsItsBytes()
    {
        // Arrange
        byte[] archive = SpriteFiles.Fpac(("scr_brg.bin", [1, 2, 3]), ("scr_brgea.bin", [4, 5]));
        _fileSystemMock.FileExists(Archive).Returns(true);
        _fileSystemMock.ReadAllBytes(Archive).Returns(archive);
        ScriptFileFinder finder = CreateScriptFileFinder();

        // Act
        byte[]? bytes = finder.Find("brg", effects: true);

        // Assert
        Assert.Equal([4, 5], bytes);
    }

    [Fact]
    public void Find_BinaryInTheDevFolder_PrefersIt()
    {
        // Arrange
        string devBinary = DevFolder.ForMod(ModFolder).Script("brg", effects: false).Binary;
        _fileSystemMock.FileExists(devBinary).Returns(true);
        _fileSystemMock.ReadAllBytes(devBinary).Returns([9, 9]);
        ScriptFileFinder finder = CreateScriptFileFinder();

        // Act
        byte[]? bytes = finder.Find("brg", effects: false);

        // Assert
        Assert.Equal([9, 9], bytes);
    }

    [Fact]
    public void Find_ScriptNowhere_ReturnsNull()
    {
        // Arrange
        ScriptFileFinder finder = CreateScriptFileFinder();

        // Act
        byte[]? bytes = finder.Find("xyz", effects: false);

        // Assert
        Assert.Null(bytes);
    }

    public ScriptFileFinder CreateScriptFileFinder() => new(_fileSystemMock, DevFolder.ForMod(ModFolder), [GameData]);
}
