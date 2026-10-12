using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Sprites;

public class CharacterSpritesTests
{
    private const string ModData = @"D:\BBTAG\mods\BBTAG-OverHaul\data";
    private const string GameData = @"D:\BBTAG\data";
    private const string RagnaInGame = @"D:\BBTAG\data\char\char_brg_img.pac";
    private const string RagnaInMod = @"D:\BBTAG\mods\BBTAG-OverHaul\data\char\char_brg_img.pac";
    private const string KohakuInMod = @"D:\BBTAG\mods\BBTAG-OverHaul\data\char\char_mko_img.pac";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();
    private readonly ILog _logMock = Substitute.For<ILog>();

    public CharacterSprites CreateCharacterSprites() => new(_fileSystemMock, [ModData, GameData], _logMock);

    [Fact]
    public void Load_GameCharacterInTheGameData_ReturnsItsIdleSpriteCropped()
    {
        // Arrange
        CharacterSprites sprites = CreateCharacterSprites();
        ArchiveAt(RagnaInGame, SpriteFiles.Fpac(
            ("rg001_00.hip", SpriteFiles.Hip(1, 1, (9, 1))),
            ("rg000_00.hip", SpriteFiles.Hip(3, 1, (0, 1), (7, 2)))));

        // Act
        IndexedSprite? sprite = sprites.Load("Ragna");

        // Assert
        Assert.Equal([7, 7], sprite?.Indices);
    }

    [Fact]
    public void Load_ModWithTheSameArchive_ReadsTheModArchive()
    {
        // Arrange
        CharacterSprites sprites = CreateCharacterSprites();
        ArchiveAt(RagnaInGame, SpriteFiles.Fpac(("rg000_00.hip", SpriteFiles.Hip(1, 1, (7, 1)))));
        ArchiveAt(RagnaInMod, SpriteFiles.Fpac(("rg000_00.hip", SpriteFiles.Hip(1, 1, (8, 1)))));

        // Act
        IndexedSprite? sprite = sprites.Load("Ragna");

        // Assert
        Assert.Equal([8], sprite?.Indices);
    }

    [Fact]
    public void Load_ModdedCharacterFolder_ReadsTheArchiveOfItsCode()
    {
        // Arrange
        CharacterSprites sprites = CreateCharacterSprites();
        ArchiveAt(KohakuInMod, SpriteFiles.Fpac(("mko000_00.hip", SpriteFiles.Hip(1, 1, (4, 1)))));

        // Act
        IndexedSprite? sprite = sprites.Load("mko");

        // Assert
        Assert.Equal([4], sprite?.Indices);
    }

    [Fact]
    public void Load_EncryptedArchive_ReturnsNullAndLogsWhy()
    {
        // Arrange
        CharacterSprites sprites = CreateCharacterSprites();
        byte[] encrypted = new byte[0x40];
        encrypted[0] = 0x7A;
        ArchiveAt(RagnaInGame, encrypted);

        // Act
        IndexedSprite? sprite = sprites.Load("Ragna");

        // Assert
        Assert.Null(sprite);
        _logMock.Received(1).Info(Arg.Is<string>(line => line.Contains("char_brg_img.pac")));
    }

    [Fact]
    public void Load_NoArchiveAnywhere_ReturnsNull()
    {
        // Arrange
        CharacterSprites sprites = CreateCharacterSprites();

        // Act
        IndexedSprite? sprite = sprites.Load("Ragna");

        // Assert
        Assert.Null(sprite);
    }

    [Fact]
    public void Load_NumberedFolder_ReturnsNullWithoutLookingForFiles()
    {
        // Arrange
        CharacterSprites sprites = CreateCharacterSprites();

        // Act
        IndexedSprite? sprite = sprites.Load("Character 140");

        // Assert
        Assert.Null(sprite);
        _fileSystemMock.DidNotReceive().FileExists(Arg.Any<string>());
    }

    private void ArchiveAt(string path, byte[] archive)
    {
        _fileSystemMock.FileExists(path).Returns(true);
        _fileSystemMock.ReadBytes(path, Arg.Any<long>(), Arg.Any<int>())
            .Returns(call => archive.AsSpan((int)call.ArgAt<long>(1), call.ArgAt<int>(2)).ToArray());
    }
}
