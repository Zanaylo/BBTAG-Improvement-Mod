using BBTAG_IM.Core.Buttons;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Tests.Sprites;

namespace BBTAG_IM.Core.Tests.Buttons;

public class ButtonIconsTests
{
    private const int AtlasWidth = ButtonIcons.CellSize * ButtonIcons.Columns;

    [Fact]
    public void ECell_Resource_IsOneColorCell()
    {
        // Act
        byte[] cell = ButtonIcons.ECell();

        // Assert
        Assert.Equal(ButtonIcons.CellSize * ButtonIcons.CellSize * 4, cell.Length);
    }

    [Fact]
    public void E_Icon_IsAnUnusedCellOfTheGameAtlas()
    {
        // Act
        (string Tag, int Icon) e = (ButtonIcons.ETag, ButtonIcons.EIcon);

        // Assert
        Assert.Equal(("CMDS", 0x172), e);
    }

    [Fact]
    public void Paste_Cell_LandsAtItsRowAndColumn()
    {
        // Arrange
        ColorImage atlas = new(AtlasWidth, ButtonIcons.CellSize * 2, new byte[AtlasWidth * ButtonIcons.CellSize * 2 * 4]);
        byte[] cell = Enumerable.Repeat((byte)0xAB, ButtonIcons.CellSize * ButtonIcons.CellSize * 4).ToArray();

        // Act
        ColorImage pasted = ButtonIcons.Paste(atlas, ButtonIcons.Columns + 2, cell);

        // Assert
        int firstPixel = (ButtonIcons.CellSize * AtlasWidth + 2 * ButtonIcons.CellSize) * 4;
        int lastPixel = ((2 * ButtonIcons.CellSize - 1) * AtlasWidth + 3 * ButtonIcons.CellSize - 1) * 4;
        Assert.Equal((0xAB, 0xAB, 0x00), (pasted.Bgra[firstPixel], pasted.Bgra[lastPixel], pasted.Bgra[firstPixel - 4]));
    }

    [Fact]
    public void Paste_Cell_LeavesTheGivenAtlasAlone()
    {
        // Arrange
        ColorImage atlas = new(AtlasWidth, ButtonIcons.CellSize, new byte[AtlasWidth * ButtonIcons.CellSize * 4]);
        byte[] cell = Enumerable.Repeat((byte)0xAB, ButtonIcons.CellSize * ButtonIcons.CellSize * 4).ToArray();

        // Act
        ButtonIcons.Paste(atlas, 0, cell);

        // Assert
        Assert.All(atlas.Bgra, value => Assert.Equal(0, value));
    }

    [Fact]
    public void Paste_CellOutsideTheAtlas_ThrowsInvalidSpriteDataException()
    {
        // Arrange
        ColorImage atlas = new(AtlasWidth, ButtonIcons.CellSize, new byte[AtlasWidth * ButtonIcons.CellSize * 4]);

        // Act
        Exception? exception = Record.Exception(() => ButtonIcons.Paste(atlas, ButtonIcons.EIcon, ButtonIcons.ECell()));

        // Assert
        Assert.IsType<InvalidSpriteDataException>(exception);
    }

    [Fact]
    public void Patch_GameArchive_ReplacesTheNestedIconAtlas()
    {
        // Arrange
        byte[] atlas = ColorHipTests.Hip(AtlasWidth, 32 * ButtonIcons.CellSize, Runs(AtlasWidth * 32 * ButtonIcons.CellSize));
        byte[] ads = LooseArchive.Write([new ArchiveFile("ads_a.hip", [1, 2, 3]), new ArchiveFile("icon.hip", atlas)]);
        byte[] archive = LooseArchive.Write([new ArchiveFile("fhd_ads.pac", ads), new ArchiveFile("other.hip", [4])]);

        // Act
        byte[] patched = ButtonIcons.Patch(archive, ButtonIcons.ECell());

        // Assert
        ArchiveFile icon = LooseArchive.Read(LooseArchive.Read(patched).Single(file => file.Name == "fhd_ads.pac").Bytes).Single(file => file.Name == "icon.hip");
        ColorImage image = ColorHip.Decode(icon.Bytes);
        int column = ButtonIcons.EIcon % ButtonIcons.Columns;
        int row = ButtonIcons.EIcon / ButtonIcons.Columns;
        int center = (((row * ButtonIcons.CellSize) + 31) * AtlasWidth + column * ButtonIcons.CellSize + 31) * 4;
        Assert.Equal(ButtonIcons.ECell()[(31 * ButtonIcons.CellSize + 31) * 4], image.Bgra[center]);
    }

    [Fact]
    public void Patch_GameArchive_KeepsTheOtherFiles()
    {
        // Arrange
        byte[] atlas = ColorHipTests.Hip(AtlasWidth, 32 * ButtonIcons.CellSize, Runs(AtlasWidth * 32 * ButtonIcons.CellSize));
        byte[] ads = LooseArchive.Write([new ArchiveFile("icon.hip", atlas)]);
        byte[] archive = LooseArchive.Write([new ArchiveFile("fhd_ads.pac", ads), new ArchiveFile("other.hip", [4])]);

        // Act
        byte[] patched = ButtonIcons.Patch(archive, ButtonIcons.ECell());

        // Assert
        Assert.Equal([4], LooseArchive.Read(patched).Single(file => file.Name == "other.hip").Bytes);
    }

    [Fact]
    public void Patch_ArchiveWithoutTheAtlas_ThrowsInvalidSpriteDataException()
    {
        // Arrange
        byte[] archive = LooseArchive.Write([new ArchiveFile("other.hip", [4])]);

        // Act
        Exception? exception = Record.Exception(() => ButtonIcons.Patch(archive, ButtonIcons.ECell()));

        // Assert
        Assert.IsType<InvalidSpriteDataException>(exception);
    }

    private static byte[] Runs(int pixels)
    {
        List<byte> runs = [];

        for (int left = pixels; left > 0; left -= 255)
            runs.AddRange([0, 0, 0, 0, (byte)Math.Min(255, left)]);

        return [.. runs];
    }
}
