using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Palettes;

public class CharSelectArchiveTests
{
    private const int Ragna = 0;

    [Fact]
    public void Patch_PaletteForAColor_ReplacesTheColorsOfThatEntryAndKeepsItsHeader()
    {
        // Arrange
        byte[] archive = SpriteFiles.Fpac(("brg00_00.hpl", SpriteFiles.Hpl(0x11)), ("brg01_00.hpl", SpriteFiles.Hpl(0x22)));
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(Ragna, fill: 0x77));

        // Act
        byte[] patched = CharSelectArchive.Patch(archive, new Dictionary<int, ImplPalette> { [1] = palette });

        // Assert
        IReadOnlyList<FpacEntry> entries = FpacArchive.Entries(patched);
        byte[] second = patched.AsSpan((int)entries[1].Offset, entries[1].Size).ToArray();
        Assert.Equal(SpriteFiles.Hpl(0x22)[..SpriteFiles.HplHeaderLength], second[..SpriteFiles.HplHeaderLength]);
        Assert.All(second[SpriteFiles.HplHeaderLength..], value => Assert.Equal(0x77, value));
    }

    [Fact]
    public void Patch_ColorWithoutPalette_LeavesItsEntryAsItWas()
    {
        // Arrange
        byte[] archive = SpriteFiles.Fpac(("brg00_00.hpl", SpriteFiles.Hpl(0x11)), ("brg01_00.hpl", SpriteFiles.Hpl(0x22)));
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(Ragna, fill: 0x77));

        // Act
        byte[] patched = CharSelectArchive.Patch(archive, new Dictionary<int, ImplPalette> { [1] = palette });

        // Assert
        IReadOnlyList<FpacEntry> entries = FpacArchive.Entries(patched);
        Assert.Equal(SpriteFiles.Hpl(0x11), patched.AsSpan((int)entries[0].Offset, entries[0].Size).ToArray());
    }

    [Fact]
    public void Patch_EntryThatIsNotAPalette_LeavesItAsItWas()
    {
        // Arrange
        byte[] notAPalette = new byte[SpriteFiles.HplHeaderLength + SpriteFiles.HplColorsLength];
        byte[] archive = SpriteFiles.Fpac(("brg00_00.hpl", notAPalette));
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(Ragna, fill: 0x77));

        // Act
        byte[] patched = CharSelectArchive.Patch(archive, new Dictionary<int, ImplPalette> { [0] = palette });

        // Assert
        Assert.Equal(archive, patched);
    }

    [Fact]
    public void Patch_FileThatIsNotAnArchive_ThrowsInvalidSpriteDataException()
    {
        // Arrange
        byte[] encrypted = new byte[0x40];
        encrypted[0] = 0x7A;

        // Act
        Exception? exception = Record.Exception(() => CharSelectArchive.Patch(encrypted, new Dictionary<int, ImplPalette>()));

        // Assert
        Assert.IsType<InvalidSpriteDataException>(exception);
    }

    [Theory]
    [InlineData(@"data\etc\char_select_pal\char_select_brg_pal.pac", "brg")]
    [InlineData("D:/BBTAG/mods/Pack/data/etc/char_select_pal/CHAR_SELECT_mko_PAL.pac", "mko")]
    public void CodeOf_CharacterSelectPalettePath_ReturnsTheCharacterCode(string path, string expected)
    {
        // Act
        string? code = CharSelectArchive.CodeOf(path);

        // Assert
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData(@"data\char\char_brg_pal.pac")]
    [InlineData(@"data\etc\char_select_dot.pac")]
    [InlineData(null)]
    public void CodeOf_OtherFile_ReturnsNull(string? path)
    {
        // Act
        string? code = CharSelectArchive.CodeOf(path);

        // Assert
        Assert.Null(code);
    }
}
