using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class CharacterImagesTests
{
    private const string ModData = @"D:\BBTAG\mods\Overhaul\data";
    private const string GameData = @"D:\BBTAG\data";
    private const string GameArchive = @"D:\BBTAG\data\char\char_brg_img.pac";
    private const string TwiceSizeArchive = @"D:\BBTAG\data\char2x\char_brg_img.pac";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Load_ImageOfTheCharacter_ReturnsItWithItsPlace()
    {
        // Arrange
        Archive(GameArchive, ("rg211_11.hip", SpriteFiles.HipAt(32, 210, 2, 1, (4, 2))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char, "rg211_11.bmp");

        // Assert
        Assert.NotNull(image);
        Assert.False(image.IsEffect);
        Assert.Equal((32, 210, 2, 1), (image.Sprite.X, image.Sprite.Y, image.Sprite.Image.Width, image.Sprite.Image.Height));
    }

    [Fact]
    public void Load_EffectImage_ComesFromTheEffectArchive()
    {
        // Arrange
        Archive(GameArchive, ("rg211_11.hip", SpriteFiles.Hip(1, 1, (4, 1))));
        Archive(@"D:\BBTAG\data\char\char_brg_vri.pac", ("vrrgef402atk_00.hip", SpriteFiles.HipAt(3, 4, 1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char, "vrrgef402atk_00.bmp");

        // Assert
        Assert.NotNull(image);
        Assert.True(image.IsEffect);
        Assert.Equal((3, 4), (image.Sprite.X, image.Sprite.Y));
    }

    [Fact]
    public void Load_ArchiveInAModFolder_WinsOverTheGame()
    {
        // Arrange
        Archive(GameArchive, ("rg211_11.hip", SpriteFiles.HipAt(0, 0, 1, 1, (4, 1))));
        Archive(@"D:\BBTAG\mods\Overhaul\data\char\char_brg_img.pac", ("rg211_11.hip", SpriteFiles.HipAt(7, 8, 1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char, "rg211_11.bmp");

        // Assert
        Assert.Equal((7, 8), (image?.Sprite.X, image?.Sprite.Y));
    }

    [Fact]
    public void Load_ImageNoArchiveHas_ReturnsNull()
    {
        // Arrange
        Archive(GameArchive, ("rg000_00.hip", SpriteFiles.Hip(1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char, "rg211_11.bmp");

        // Assert
        Assert.Null(image);
    }

    [Fact]
    public void Load_CharacterWithoutAnImageArchive_ReturnsNull()
    {
        // Arrange
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char, "rg211_11.bmp");

        // Assert
        Assert.Null(image);
    }

    [Fact]
    public void Load_SameImageTwice_ReadsItOnce()
    {
        // Arrange
        Archive(GameArchive, ("rg211_11.hip", SpriteFiles.Hip(1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();
        images.Load("brg", SpriteFolder.Char, "rg211_11.bmp");
        _fileSystemMock.ClearReceivedCalls();

        // Act
        images.Load("brg", SpriteFolder.Char, "rg211_11.bmp");

        // Assert
        _fileSystemMock.DidNotReceive().ReadBytes(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<int>());
    }

    [Fact]
    public void Load_ImageOnlyInTheTwiceSizeFolder_FindsItThere()
    {
        // Arrange
        Archive(TwiceSizeArchive, ("ar000_00.hip", SpriteFiles.HipAt(82, 216, 1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char, "ar000_00.bmp");

        // Assert
        Assert.Equal((82, 216), (image?.Sprite.X, image?.Sprite.Y));
    }

    [Fact]
    public void Load_ImageInTheCharacterFolderAndInAnother_TakesTheCharacterFolder()
    {
        // Arrange
        Archive(GameArchive, ("rg211_11.hip", SpriteFiles.HipAt(1, 1, 1, 1, (4, 1))));
        Archive(TwiceSizeArchive, ("rg211_11.hip", SpriteFiles.HipAt(2, 2, 1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char2x, "rg211_11.bmp");

        // Assert
        Assert.Equal((2, 2), (image?.Sprite.X, image?.Sprite.Y));
    }

    [Fact]
    public void Load_ImageOnlyInTheExtraArchiveOfTheFolder_FindsIt()
    {
        // Arrange
        Archive(TwiceSizeArchive, ("rg000_00.hip", SpriteFiles.Hip(1, 1, (4, 1))));
        Archive(@"D:\BBTAG\data\char2x_add_ex\char_brg_img.pac", ("rg999_00.hip", SpriteFiles.HipAt(5, 6, 1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char2x, "rg999_00.bmp");

        // Assert
        Assert.Equal((5, 6), (image?.Sprite.X, image?.Sprite.Y));
    }

    [Fact]
    public void Load_EmptyArchiveInTheCharacterFolder_LooksInTheNextOne()
    {
        // Arrange
        Archive(TwiceSizeArchive);
        Archive(GameArchive, ("rg211_11.hip", SpriteFiles.HipAt(3, 3, 1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char2x, "rg211_11.bmp");

        // Assert
        Assert.Equal((3, 3), (image?.Sprite.X, image?.Sprite.Y));
    }

    [Fact]
    public void Load_ImageThatCannotBeDrawnInTheCharacterFolder_LooksInTheNextOne()
    {
        // Arrange
        Archive(TwiceSizeArchive, ("rg211_11.hip", SpriteFiles.Hip(2, 2, (4, 1))));
        Archive(GameArchive, ("rg211_11.hip", SpriteFiles.HipAt(3, 3, 1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char2x, "rg211_11.bmp");

        // Assert
        Assert.Equal((3, 3), (image?.Sprite.X, image?.Sprite.Y));
    }

    [Theory]
    [InlineData(@"D:\BBTAG\data\char\char_brg_img.pac", 1f)]
    [InlineData(@"D:\BBTAG\data\charfhd\char_brg_img.pac", 1.5f)]
    [InlineData(@"D:\BBTAG\data\char2x\char_brg_img.pac", 2f)]
    [InlineData(@"D:\BBTAG\data\char2x_add_ex\char_brg_img.pac", 2f)]
    public void Load_ImageInAFolder_UsesTheSizeTheGameDrawsThatFolderAt(string archive, float pixelsPerUnit)
    {
        // Arrange
        Archive(archive, ("rg211_11.hip", SpriteFiles.Hip(1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char, "rg211_11.bmp");

        // Assert
        Assert.Equal(pixelsPerUnit, image?.PixelsPerUnit);
    }

    [Fact]
    public void Load_EffectImageInTheTwiceSizeFolder_UsesTwoPixelsPerUnit()
    {
        // Arrange
        Archive(@"D:\BBTAG\data\char2x\char_brg_vri.pac", ("vr_ef000.hip", SpriteFiles.Hip(1, 1, (4, 1))));
        CharacterImages images = CreateCharacterImages();

        // Act
        CharacterImage? image = images.Load("brg", SpriteFolder.Char2x, "vr_ef000.bmp");

        // Assert
        Assert.Equal(2f, image?.PixelsPerUnit);
    }

    private CharacterImages CreateCharacterImages() => new(_fileSystemMock, [ModData, GameData]);

    private void Archive(string path, params (string Name, byte[] Bytes)[] files)
    {
        byte[] archive = SpriteFiles.Fpac(files);
        _fileSystemMock.FileExists(path).Returns(true);
        _fileSystemMock.ReadBytes(path, Arg.Any<long>(), Arg.Any<int>())
            .Returns(call => archive.AsSpan((int)call.ArgAt<long>(1), Math.Min(call.ArgAt<int>(2), archive.Length - (int)call.ArgAt<long>(1))).ToArray());
    }
}
