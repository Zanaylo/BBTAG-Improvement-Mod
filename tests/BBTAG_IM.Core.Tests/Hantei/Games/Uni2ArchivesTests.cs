using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Hantei.Games;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Hantei.Games;

public class Uni2ArchivesTests
{
    private const string Root = @"X:\uni2";
    private const string DataIndex = @"X:\uni2\d\" + Uni2Archives.DataIndex;
    private const string EnglishIndex = @"X:\uni2\d\" + Uni2Archives.EnglishIndex;
    private const string DataArchive = @"X:\uni2\d\ehex";

    private readonly IFileSystem _files = Substitute.For<IFileSystem>();

    [Fact]
    public void Open_FolderEntry_NamesItsFilesFromTheGameFolder()
    {
        // Arrange
        ArrangeIndexes();

        // Act
        IHanteiGameFiles game = Uni2Archives.Open(_files, Root);

        // Assert
        Assert.Contains("data/chr000/chr000.ha6", game.Names);
    }

    [Fact]
    public void Open_SecondFolder_KeepsCountingEntriesFromTheFirst()
    {
        // Arrange
        ArrangeIndexes();
        byte[] bytes = [1, 2, 3, 4, 5, 6];
        _files.ReadBytes(DataArchive, 30, 6).Returns(bytes);

        // Act
        byte[] read = Uni2Archives.Open(_files, Root).Read("data/chr001/chr001.ha6");

        // Assert
        Assert.Equal(bytes, read);
    }

    [Fact]
    public void Open_EnglishIndex_AddsTheCharacterNames()
    {
        // Arrange
        ArrangeIndexes();

        // Act
        IHanteiGameFiles game = Uni2Archives.Open(_files, Root);

        // Assert
        Assert.True(game.Contains(@"___English\System\BtlCharaTbl_str.ini"));
    }

    [Fact]
    public void Open_NoEnglishIndex_StillReadsTheData()
    {
        // Arrange
        ArrangeIndexes();
        _files.FileExists(EnglishIndex).Returns(false);

        // Act
        IHanteiGameFiles game = Uni2Archives.Open(_files, Root);

        // Assert
        Assert.True(game.Contains("data/chr000/chr000.ha6"));
    }

    [Fact]
    public void Open_CutIndex_Refuses()
    {
        // Arrange
        ArrangeIndexes();
        _files.ReadAllBytes(DataIndex).Returns(new byte[20]);

        // Act
        Exception? exception = Record.Exception(() => Uni2Archives.Open(_files, Root));

        // Assert
        Assert.IsType<InvalidHanteiDataException>(exception);
    }

    [Fact]
    public void Open_NamesDifferOnlyInCase_FindsTheFile()
    {
        // Arrange
        ArrangeIndexes();

        // Act
        IHanteiGameFiles game = Uni2Archives.Open(_files, Root);

        // Assert
        Assert.True(game.Contains("DATA/CHR000/CHR000.HA6"));
    }

    private void ArrangeIndexes()
    {
        _files.FileExists(DataIndex).Returns(true);
        _files.FileExists(EnglishIndex).Returns(true);
        _files.ReadAllBytes(DataIndex).Returns(Uni2IndexBytes.Build(
            "ehex",
            (@"data\chr000", [("chr000.ha6", 0, 10)]),
            (@"data\chr001", [("chr001.ha6", 30, 6)])));
        _files.ReadAllBytes(EnglishIndex).Returns(Uni2IndexBytes.Build(
            "words",
            (@"___English\System", [("BtlCharaTbl_str.ini", 0, 12)])));
    }
}
