using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Hantei.Games;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Hantei.Games;

public class HanteiGamesTests
{
    private readonly IFileSystem _files = Substitute.For<IFileSystem>();

    [Fact]
    public void Open_Uni2Executable_ReadsItsArchives()
    {
        // Arrange
        _files.FileExists(@"X:\uni2\uni2.exe").Returns(true);
        _files.FileExists(@"X:\uni2\d\" + Uni2Archives.DataIndex).Returns(true);
        _files.ReadAllBytes(@"X:\uni2\d\" + Uni2Archives.DataIndex).Returns(Uni2IndexBytes.Build("ehex", (@"data\chr000", [("chr000.ha6", 0, 10)])));

        // Act
        HanteiGame game = HanteiGames.Open(@"X:\uni2\uni2.exe", _files);

        // Assert
        Assert.Equal(HanteiGames.Uni2Title, game.Title);
        Assert.Equal(@"X:\uni2", game.Root);
    }

    [Fact]
    public void Open_UnpackedTemplateFile_UsesTheFolderAboveData()
    {
        // Arrange
        _files.FileExists(@"X:\unpacked\data\BaseData.HA6").Returns(true);
        _files.DirectoryExists(@"X:\unpacked\data").Returns(true);

        // Act
        HanteiGame game = HanteiGames.Open(@"X:\unpacked\data\BaseData.HA6", _files);

        // Assert
        Assert.Equal(@"X:\unpacked", game.Root);
        Assert.Equal("unpacked", game.Title);
    }

    [Fact]
    public void Open_UnpackedFolder_ReadsItsFiles()
    {
        // Arrange
        _files.DirectoryExists(@"X:\unpacked\data").Returns(true);
        _files.EnumerateFilesRecursively(@"X:\unpacked\data").Returns([@"X:\unpacked\data\chr000\chr000.ha6"]);
        _files.FileExists(@"X:\unpacked\data\chr000\chr000.ha6").Returns(true);
        _files.ReadAllBytes(@"X:\unpacked\data\chr000\chr000.ha6").Returns([1, 2]);

        // Act
        HanteiGame game = HanteiGames.Open(@"X:\unpacked", _files);

        // Assert
        Assert.Equal(["data/chr000/chr000.ha6"], game.Files.Names);
        Assert.Equal([1, 2], game.Files.Read("data/chr000/chr000.ha6"));
    }

    [Fact]
    public void Open_NothingKnown_Refuses()
    {
        // Arrange
        _files.FileExists(@"X:\other\game.exe").Returns(true);

        // Act
        Exception? exception = Record.Exception(() => HanteiGames.Open(@"X:\other\game.exe", _files));

        // Assert
        Assert.IsType<InvalidHanteiDataException>(exception);
    }
}
