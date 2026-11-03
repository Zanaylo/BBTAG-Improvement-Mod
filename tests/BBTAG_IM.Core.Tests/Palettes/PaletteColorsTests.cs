using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Palettes;

public class PaletteColorsTests
{
    private const int EffectFile = 3;
    private const int Entry = 17;

    public PaletteColors CreatePaletteColors() => new();

    [Fact]
    public void Get_BlueGreenRedAlphaBytes_ReturnsTheColor()
    {
        // Arrange
        byte[] bytes = new byte[PaletteColors.Length];
        int at = EffectFile * PaletteColors.FileLength + Entry * PaletteColors.BytesPerColor;
        bytes[at] = 0x30;
        bytes[at + 1] = 0x20;
        bytes[at + 2] = 0x10;
        bytes[at + 3] = 0x80;
        PaletteColors colors = PaletteColors.From(bytes);

        // Act
        PaletteColor color = colors.Get(EffectFile, Entry);

        // Assert
        Assert.Equal(new PaletteColor(0x10, 0x20, 0x30, 0x80), color);
    }

    [Fact]
    public void Set_Color_StoresItAsBlueGreenRedAlpha()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();

        // Act
        colors.Set(EffectFile, Entry, new PaletteColor(0x10, 0x20, 0x30, 0x80));

        // Assert
        int at = EffectFile * PaletteColors.FileLength + Entry * PaletteColors.BytesPerColor;
        Assert.Equal(new byte[] { 0x30, 0x20, 0x10, 0x80 }, colors.Bytes.Slice(at, PaletteColors.BytesPerColor).ToArray());
    }

    [Fact]
    public void Copy_ChangedAfterwards_LeavesTheCopyAlone()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        PaletteColors copy = colors.Copy();

        // Act
        colors.Set(0, Entry, new PaletteColor(1, 2, 3, 4));

        // Assert
        Assert.Equal(default, copy.Get(0, Entry));
    }

    [Fact]
    public void SameAs_EqualBytes_ReturnsTrue()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        colors.Set(0, Entry, new PaletteColor(1, 2, 3, 4));
        PaletteColors other = colors.Copy();

        // Act
        bool same = colors.SameAs(other);

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void SameAs_OneColorDiffers_ReturnsFalse()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        PaletteColors other = colors.Copy();
        other.Set(EffectFile, Entry, new PaletteColor(1, 2, 3, 4));

        // Act
        bool same = colors.SameAs(other);

        // Assert
        Assert.False(same);
    }

    [Fact]
    public void Flashing_Lit_PaintsTheEntryMagenta()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        colors.Set(EffectFile, Entry, new PaletteColor(10, 20, 30, 255));

        // Act
        PaletteColors flashing = colors.Flashing(EffectFile, [Entry], lit: true);

        // Assert
        Assert.Equal(new PaletteColor(255, 0, 255, 255), flashing.Get(EffectFile, Entry));
    }

    [Fact]
    public void Flashing_Unlit_PaintsTheEntryCyan()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        colors.Set(EffectFile, Entry, new PaletteColor(10, 20, 30, 255));

        // Act
        PaletteColors flashing = colors.Flashing(EffectFile, [Entry], lit: false);

        // Assert
        Assert.Equal(new PaletteColor(0, 255, 255, 255), flashing.Get(EffectFile, Entry));
    }

    [Fact]
    public void Flashing_SeveralEntries_PaintsEachOfThem()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        colors.Set(EffectFile, Entry + 2, new PaletteColor(10, 20, 30, 255));

        // Act
        PaletteColors flashing = colors.Flashing(EffectFile, [Entry, Entry + 2], lit: true);

        // Assert
        Assert.Equal(new PaletteColor(255, 0, 255, 255), flashing.Get(EffectFile, Entry + 2));
    }

    [Fact]
    public void Flashing_SeeThroughEntry_KeepsItsTransparency()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        colors.Set(EffectFile, Entry, new PaletteColor(10, 20, 30, 0x40));

        // Act
        PaletteColors flashing = colors.Flashing(EffectFile, [Entry], lit: true);

        // Assert
        Assert.Equal(0x40, flashing.Get(EffectFile, Entry).A);
    }

    [Fact]
    public void Flashing_Entry_DimsTheOtherEntriesToDarkGrey()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        colors.Set(EffectFile, Entry + 1, new PaletteColor(200, 100, 50, 255));

        // Act
        PaletteColors flashing = colors.Flashing(EffectFile, [Entry], lit: true);

        // Assert
        Assert.Equal(new PaletteColor(62, 62, 62, 255), flashing.Get(EffectFile, Entry + 1));
    }

    [Fact]
    public void Flashing_Entry_DimsTheOtherFilesToo()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        colors.Set(0, Entry, new PaletteColor(255, 255, 255, 255));

        // Act
        PaletteColors flashing = colors.Flashing(EffectFile, [Entry], lit: true);

        // Assert
        Assert.Equal(new PaletteColor(127, 127, 127, 255), flashing.Get(0, Entry));
    }

    [Fact]
    public void Flashing_SeeThroughOtherEntry_KeepsItsTransparency()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        colors.Set(EffectFile, Entry + 1, new PaletteColor(200, 100, 50, 0x40));

        // Act
        PaletteColors flashing = colors.Flashing(EffectFile, [Entry], lit: true);

        // Assert
        Assert.Equal(0x40, flashing.Get(EffectFile, Entry + 1).A);
    }

    [Fact]
    public void Flashing_Entry_LeavesTheColorsItWasMadeFromAlone()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        colors.Set(EffectFile, Entry, new PaletteColor(10, 20, 30, 255));

        // Act
        colors.Flashing(EffectFile, [Entry], lit: true);

        // Assert
        Assert.Equal(new PaletteColor(10, 20, 30, 255), colors.Get(EffectFile, Entry));
    }

    [Fact]
    public void Wearing_PaletteWithAnEmptyFile_KeepsTheseColorsForThatFile()
    {
        // Arrange
        PaletteColors colors = CreatePaletteColors();
        colors.Set(EffectFile, Entry, new PaletteColor(1, 2, 3, 4));
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(24, fill: 0x42).WithEmptyFile(EffectFile));

        // Act
        PaletteColors worn = colors.Wearing(palette);

        // Assert
        Assert.Equal(new PaletteColor(1, 2, 3, 4), worn.Get(EffectFile, Entry));
        Assert.Equal(new PaletteColor(0x42, 0x42, 0x42, 0x42), worn.Get(0, Entry));
    }
}
