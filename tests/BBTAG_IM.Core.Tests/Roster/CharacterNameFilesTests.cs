using System.Text;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Roster;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Roster;

public class CharacterNameFilesTests
{
    private const string ModData = @"D:\BBTAG\mods\Overhaul\data";
    private const string GameData = @"D:\BBTAG\data";
    private const string Cache = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\cache";
    private const string GameList = @"D:\BBTAG\data\localize\eng_idlist.txt";

    private static readonly KeyValuePair<string, string>[] Entries = [new("Chara_NameRAR", "Red Arcueid")];

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Substitute_IdListRequest_ServesACopyWithTheNames()
    {
        // Arrange
        _fileSystemMock.FileExists(GameList).Returns(true);
        _fileSystemMock.ReadAllBytes(GameList).Returns(List("A\r\nB\r\n/// End Of Text\r\n"));
        CharacterNameFiles files = CreateCharacterNameFiles(Entries);

        // Act
        string? substitute = files.Substitute("data/localize/eng_idlist.txt");

        // Assert
        Assert.Equal(Cache + @"\eng_idlist.txt", substitute);
        _fileSystemMock.Received(1).WriteAllBytes(Cache + @"\eng_idlist.txt", Arg.Is<byte[]>(bytes => bytes.SequenceEqual(List("A\r\nB\r\nChara_NameRAR\r\nRed Arcueid\r\n/// End Of Text\r\n"))));
    }

    [Fact]
    public void Substitute_ModReplacesTheList_PatchesTheModCopy()
    {
        // Arrange
        string modList = ModData + @"\localize\eng_idlist.txt";
        _fileSystemMock.FileExists(modList).Returns(true);
        _fileSystemMock.FileExists(GameList).Returns(true);
        _fileSystemMock.ReadAllBytes(modList).Returns(List("/// End Of Text\r\n"));
        CharacterNameFiles files = CreateCharacterNameFiles(Entries);

        // Act
        files.Substitute("data/localize/eng_idlist.txt");

        // Assert
        _fileSystemMock.DidNotReceive().ReadAllBytes(GameList);
    }

    [Fact]
    public void Substitute_SecondRequest_ReusesTheCopy()
    {
        // Arrange
        _fileSystemMock.FileExists(GameList).Returns(true);
        _fileSystemMock.ReadAllBytes(GameList).Returns(List("/// End Of Text\r\n"));
        CharacterNameFiles files = CreateCharacterNameFiles(Entries);
        files.Substitute("data/localize/eng_idlist.txt");
        _fileSystemMock.FileExists(Cache + @"\eng_idlist.txt").Returns(true);

        // Act
        files.Substitute("data/localize/eng_idlist.txt");

        // Assert
        _fileSystemMock.Received(1).WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Theory]
    [InlineData("data/localize/eng_glyph.hip")]
    [InlineData("data/localize/cmn_idlist.txt")]
    [InlineData("data/char/char_brg_scr.pac")]
    public void Substitute_OtherFile_ReturnsNull(string requested)
    {
        // Arrange
        CharacterNameFiles files = CreateCharacterNameFiles(Entries);

        // Act
        string? substitute = files.Substitute(requested);

        // Assert
        Assert.Null(substitute);
    }

    [Fact]
    public void Substitute_NoNewCharacters_ReturnsNull()
    {
        // Arrange
        _fileSystemMock.FileExists(GameList).Returns(true);
        CharacterNameFiles files = CreateCharacterNameFiles([]);

        // Act
        string? substitute = files.Substitute("data/localize/eng_idlist.txt");

        // Assert
        Assert.Null(substitute);
    }

    public CharacterNameFiles CreateCharacterNameFiles(IReadOnlyList<KeyValuePair<string, string>> entries) =>
        new(_fileSystemMock, [ModData, GameData], entries, Cache);

    private static byte[] List(string text) => [0xFF, 0xFE, .. Encoding.Unicode.GetBytes(text)];
}
