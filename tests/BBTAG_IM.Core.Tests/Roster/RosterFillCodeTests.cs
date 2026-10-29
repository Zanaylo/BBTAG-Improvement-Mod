using System.Text;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Roster;

namespace BBTAG_IM.Core.Tests.Roster;

public class RosterFillCodeTests
{
    private const uint Site = 0x00674C43;
    private const uint Block = 0x30000000;

    private static readonly byte[] FillEnd = [0xC7, 0x86, 0x04, 0x2F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x8D, 0x9E, 0x04, 0x2E, 0x00, 0x00];
    private static readonly byte[] ReplacedStore = [0xC7, 0x86, 0x04, 0x2F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    [Fact]
    public void Locate_GameCode_ReturnsTheStoreAfterTheRosterIsFilled()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(Site, FillEnd)]);

        // Act
        uint site = RosterFillCode.Locate(image);

        // Assert
        Assert.Equal(Site, site);
    }

    [Fact]
    public void Names_Addition_LaysOutCodeUpperCodeShortNameAndAlternateCode()
    {
        // Arrange
        RosterAddition addition = new(55, 0, Manifest("rar", null));

        // Act
        byte[] names = RosterFillCode.Names([addition]);

        // Assert
        Assert.Equal(
            ("rar", "RAR", "M_REDARC", "rar", RosterFillCode.NamesLength),
            (Text(names, 0), Text(names, 0x20), Text(names, 0x40), Text(names, 0x60), names.Length));
    }

    [Fact]
    public void Code_AdditionWithoutSpriteFolder_CopiesTheTemplateWritesTheNamesAndCountsIt()
    {
        // Arrange
        RosterAddition addition = new(55, 3, Manifest("rar", null));
        byte[] fill =
        [
            0x60,
            0x8D, 0xBE, .. BitConverter.GetBytes(55u * 0xB8),
            0x56,
            0x8D, 0xB6, .. BitConverter.GetBytes(3u * 0xB8),
            0xB9, 0x2E, 0x00, 0x00, 0x00,
            0xF3, 0xA5,
            0x5E,
            0x8D, 0xBE, .. BitConverter.GetBytes(55u * 0xB8),
            0x56,
            0xBE, .. BitConverter.GetBytes(Block + RosterFillCode.NamesOffset),
            0xB9, 0x20, 0x00, 0x00, 0x00,
            0xF3, 0xA5,
            0x5E,
            0xFF, 0x86, 0x00, 0x2E, 0x00, 0x00,
            0x61,
            .. ReplacedStore,
        ];
        uint jumpEnd = Block + RosterFillCode.CodeOffset + (uint)fill.Length + 5;

        // Act
        byte[] code = RosterFillCode.Code(Site, Block, [addition]);

        // Assert
        Assert.Equal([.. fill, 0xE9, .. BitConverter.GetBytes(Site + 10 - jumpEnd)], code);
    }

    [Fact]
    public void Code_AdditionWithSpriteFolder_AlsoWritesTheFolder()
    {
        // Arrange
        RosterAddition addition = new(55, 3, Manifest("rar", SpriteFolder.Char2x));
        byte[] folderStore = [0xC7, 0x86, .. BitConverter.GetBytes(55u * 0xB8 + 0xB0), .. BitConverter.GetBytes(2)];

        // Act
        byte[] code = RosterFillCode.Code(Site, Block, [addition]);

        // Assert
        Assert.True(code.AsSpan().IndexOf(folderStore) > 0);
    }

    [Fact]
    public void Code_ManyAdditions_FitsInTheBlock()
    {
        // Arrange
        RosterAddition[] additions = Enumerable.Range(55, 8).Select(index => new RosterAddition(index, 0, Manifest("rar", SpriteFolder.Char2x))).ToArray();

        // Act
        int end = RosterFillCode.CodeOffset + RosterFillCode.Code(Site, Block, additions).Length;

        // Assert
        Assert.True(end <= RosterFillCode.BlockLength);
    }

    [Fact]
    public void Jump_SiteAndBlock_JumpsToTheCodeAndPadsTheReplacedStore()
    {
        // Act
        byte[] jump = RosterFillCode.Jump(Site, Block);

        // Assert
        Assert.Equal([0xE9, .. BitConverter.GetBytes(Block + RosterFillCode.CodeOffset - (Site + 5)), 0x90, 0x90, 0x90, 0x90, 0x90], jump);
    }

    private static string Text(byte[] bytes, int offset) => Encoding.ASCII.GetString(bytes, offset, 0x20).TrimEnd('\0');

    private static CharacterManifest Manifest(string code, SpriteFolder? folder) =>
        new(code, "brg", "M_REDARC", folder, Series.Others, new CharacterNames("Red Arcueid", "Red Arc", "RED=ARCUEID"));
}
