using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Roster;

namespace BBTAG_IM.Core.Tests.Roster;

public class TemplateIndexCodeTests
{
    private const uint Site = 0x007662F2;
    private const uint Block = 0x20000000;

    private static readonly byte[] ObjectInit = [0x8B, 0x06, 0x89, 0x43, 0x34, 0x8B, 0x46, 0x08, 0x83, 0xF8, 0xFF];

    [Fact]
    public void Locate_GameCode_ReturnsWhereTheObjectStoresItsCharacter()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(Site, ObjectInit)]);

        // Act
        uint site = TemplateIndexCode.Locate(image);

        // Assert
        Assert.Equal(Site, site);
    }

    [Fact]
    public void Map_Additions_PointsNewIndexesAtTheirTemplatesAndKeepsTheOthers()
    {
        // Arrange
        RosterAddition[] additions = [new RosterAddition(55, 0, Manifest("rar")), new RosterAddition(56, 12, Manifest("rgx"))];

        // Act
        byte[] map = TemplateIndexCode.Map(additions);

        // Assert
        Assert.Equal((64, (byte)0, (byte)12, (byte)54, (byte)57), (map.Length, map[55], map[56], map[54], map[57]));
    }

    [Fact]
    public void Code_SiteAndBlock_LooksTheIndexUpInTheMapAndReturns()
    {
        // Arrange
        uint codeStart = Block + TemplateIndexCode.CodeOffset;
        byte[] lookup =
        [
            0x8B, 0x06,
            0x83, 0xF8, 0x40,
            0x73, 0x07,
            0x0F, 0xB6, 0x80, .. BitConverter.GetBytes(Block + TemplateIndexCode.MapOffset),
            0x89, 0x43, 0x34,
        ];
        uint jumpEnd = codeStart + (uint)lookup.Length + 5;

        // Act
        byte[] code = TemplateIndexCode.Code(Site, Block);

        // Assert
        Assert.Equal([.. lookup, 0xE9, .. BitConverter.GetBytes(Site + 5 - jumpEnd)], code);
    }

    [Fact]
    public void Jump_SiteAndBlock_JumpsToTheCodeOfTheBlock()
    {
        // Act
        byte[] jump = TemplateIndexCode.Jump(Site, Block);

        // Assert
        Assert.Equal([0xE9, .. BitConverter.GetBytes(Block + TemplateIndexCode.CodeOffset - (Site + 5))], jump);
    }

    private static CharacterManifest Manifest(string code) =>
        new(code, "brg", code.ToUpperInvariant(), null, Series.BlazBlue, new CharacterNames(code, code, code));
}
