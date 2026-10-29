using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Roster;

namespace BBTAG_IM.Core.Tests.Roster;

public class CharacterGridCodeTests
{
    private const uint Site = 0x006E1FBD;
    private const uint Block = 0x40000000;

    private static readonly byte[] BuilderEnd = [0xC7, 0x86, 0x3C, 0x0C, 0x00, 0x00, 0x34, 0x00, 0x00, 0x00, 0x5E, 0xC3];
    private static readonly byte[] ReplacedStore = [0xC7, 0x86, 0x3C, 0x0C, 0x00, 0x00, 0x34, 0x00, 0x00, 0x00];

    [Fact]
    public void Locate_GameCode_ReturnsTheLastStoreOfTheGrid()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(Site, BuilderEnd)]);

        // Act
        uint site = CharacterGridCode.Locate(image);

        // Assert
        Assert.Equal(Site, site);
    }

    [Fact]
    public void Code_AdditionInTheUnderNightRow_AppendsItsIndexAfterTheLastCharacterOfTheRow()
    {
        // Arrange
        RosterAddition addition = new(56, 0, Manifest(Series.UnderNight));
        byte[] append =
        [
            .. ReplacedStore,
            0x60,
            0x8D, 0xBE, .. BitConverter.GetBytes(0x830u + 2 * 0x100),
            0xB8, 0xFF, 0xFF, 0xFF, 0xFF,
            0xB9, 0x40, 0x00, 0x00, 0x00,
            0xF2, 0xAF,
            0x75, 0x07,
            0xC7, 0x47, 0xFC, .. BitConverter.GetBytes(56),
            0x61,
        ];
        uint jumpEnd = Block + (uint)append.Length + 5;

        // Act
        byte[] code = CharacterGridCode.Code(Site, Block, [addition]);

        // Assert
        Assert.Equal([.. append, 0xE9, .. BitConverter.GetBytes(Site + 10 - jumpEnd)], code);
    }

    [Theory]
    [InlineData(Series.BlazBlue, 0x830u)]
    [InlineData(Series.Persona, 0x930u)]
    [InlineData(Series.Rwby, 0xB30u)]
    [InlineData(Series.Others, 0xC30u)]
    public void Code_Series_SearchesItsRow(Series series, uint row)
    {
        // Arrange
        byte[] rowStart = [0x8D, 0xBE, .. BitConverter.GetBytes(row)];

        // Act
        byte[] code = CharacterGridCode.Code(Site, Block, [new RosterAddition(55, 0, Manifest(series))]);

        // Assert
        Assert.True(code.AsSpan().IndexOf(rowStart) > 0);
    }

    [Fact]
    public void Jump_SiteAndBlock_JumpsToTheCodeAndPadsTheReplacedStore()
    {
        // Act
        byte[] jump = CharacterGridCode.Jump(Site, Block);

        // Assert
        Assert.Equal([0xE9, .. BitConverter.GetBytes(Block - (Site + 5)), 0x90, 0x90, 0x90, 0x90, 0x90], jump);
    }

    private static CharacterManifest Manifest(Series series) =>
        new("rar", "brg", "RAR", null, series, new CharacterNames("rar", "rar", "rar"));
}
