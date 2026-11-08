using System.Text;
using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Hantei.Games;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BBTAG_IM.Core.Tests.Hantei.Games;

public class MbtlArchivesTests
{
    private const string Root = @"X:\mbtl";
    private const string Executable = @"X:\mbtl\MBTL.exe";
    private const string DataArchive = @"X:\mbtl\data008.bin";
    private const string WordsArchive = @"X:\mbtl\data000.bin";
    private const string Pattern = "Hantei6DataFile";
    private const int PatternOffset = 100;

    private static readonly byte[] Key = new byte[1024];

    private readonly IFileSystem _files = Substitute.For<IFileSystem>();

    [Fact]
    public void Open_Table_ListsTheDataAndNameFiles()
    {
        // Arrange
        ArrangeGame();

        // Act
        IHanteiGameFiles game = MbtlArchives.Open(_files, Root);

        // Assert
        Assert.Contains(MbtlIndex.AnchorName, game.Names);
        Assert.Contains("___English/System/BtlCharaTbl_str.ini", game.Names);
    }

    [Fact]
    public void Open_FileOfAnotherArchive_IsLeftOut()
    {
        // Arrange
        ArrangeGame();

        // Act
        IHanteiGameFiles game = MbtlArchives.Open(_files, Root);

        // Assert
        Assert.DoesNotContain("bg/stage.bin", game.Names);
    }

    [Fact]
    public void Read_ArchivedFile_ComesOutDecoded()
    {
        // Arrange
        ArrangeGame();
        _files.ReadBytes(WordsArchive, 500, 3).Returns([0xA5, 0x18, 7]);

        // Act
        byte[] read = MbtlArchives.Open(_files, Root).Read("___English/System/BtlCharaTbl_str.ini");

        // Assert
        Assert.Equal([0, 0, 7], read);
    }

    [Fact]
    public void Open_MissingAnchorName_Refuses()
    {
        // Arrange
        ArrangeGame();
        _files.ReadAllBytes(Executable).Returns(MbtlExeBytes.Build(Key, ("data/other.txt", 0, 4), ("data/more.txt", 4, 4)));

        // Act
        Exception? exception = Record.Exception(() => MbtlArchives.Open(_files, Root));

        // Assert
        Assert.IsType<InvalidHanteiDataException>(exception);
    }

    [Fact]
    public void Open_PatternsNotReadable_Refuses()
    {
        // Arrange
        ArrangeGame();
        _files.ReadBytes(DataArchive, PatternOffset, Pattern.Length).Returns(Enumerable.Repeat((byte)0xEE, Pattern.Length).ToArray());

        // Act
        Exception? exception = Record.Exception(() => MbtlArchives.Open(_files, Root));

        // Assert
        Assert.IsType<InvalidHanteiDataException>(exception);
    }

    [Fact]
    public void Open_MisalignedTablesAlsoChain_KeepsThePairThatReadsBack()
    {
        // Arrange
        ArrangeGame();
        _files.ReadBytes(DataArchive, Arg.Is<long>(offset => offset > PatternOffset), Arg.Any<int>()).Returns(new byte[Pattern.Length]);

        // Act
        IHanteiGameFiles game = MbtlArchives.Open(_files, Root);

        // Assert
        Assert.Equal(Pattern, Encoding.ASCII.GetString(game.Read(MbtlIndex.AnchorName)));
    }

    [Fact]
    public void Open_MisalignedTablesRunPastTheArchive_KeepsLooking()
    {
        // Arrange
        ArrangeGame();
        _files.ReadBytes(DataArchive, Arg.Is<long>(offset => offset > PatternOffset), Arg.Any<int>()).Throws(new EndOfStreamException());

        // Act
        IHanteiGameFiles game = MbtlArchives.Open(_files, Root);

        // Assert
        Assert.Equal(Pattern, Encoding.ASCII.GetString(game.Read(MbtlIndex.AnchorName)));
    }

    private void ArrangeGame()
    {
        _files.ReadAllBytes(Executable).Returns(MbtlExeBytes.Build(
            Key,
            ("bg/stage.bin", 0, PatternOffset),
            (MbtlIndex.AnchorName, PatternOffset, Pattern.Length),
            ("data/chr000/chr000_0.txt", PatternOffset + (uint)Pattern.Length, 40),
            ("data/chr000/chr000.cg", PatternOffset + (uint)Pattern.Length + 40, 60),
            ("data/chr000/chr000.pal", PatternOffset + (uint)Pattern.Length + 100, 60),
            ("___English/System/BtlCharaTbl_str.ini", 500, 3)));
        _files.ReadBytes(DataArchive, PatternOffset, Pattern.Length).Returns(Coded(Pattern));
    }

    private static byte[] Coded(string text)
    {
        byte[] plain = Encoding.ASCII.GetBytes(text);
        plain[0] ^= 0xA5;
        plain[1] ^= 0x18;
        return plain;
    }
}
