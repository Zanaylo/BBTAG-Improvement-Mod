using System.Text;
using BBTAG_IM.Core.Roster;

namespace BBTAG_IM.Core.Tests.Roster;

public class IdListTests
{
    private static readonly byte[] Trailer = [0x3A, 0x56, 0x22, 0x4F, 0x67, 0xBF];

    [Fact]
    public void Insert_NewEntry_GoesRightBeforeTheFirstSectionEnd()
    {
        // Arrange
        byte[] file = File("Chara_NameBRG\r\nRagna\r\n/// End Of Text\r\nOther\r\nText\r\n/// End Of Text\r\n");

        // Act
        byte[] patched = IdList.Insert(file, [new("Chara_NameRAR", "Red Arcueid")]);

        // Assert
        Assert.Equal(File("Chara_NameBRG\r\nRagna\r\nChara_NameRAR\r\nRed Arcueid\r\n/// End Of Text\r\nOther\r\nText\r\n/// End Of Text\r\n"), patched);
    }

    [Fact]
    public void Insert_KeyAlreadyInTheList_LeavesItAlone()
    {
        // Arrange
        byte[] file = File("Chara_NameRAR\r\nMine\r\n/// End Of Text\r\n");

        // Act
        byte[] patched = IdList.Insert(file, [new("Chara_NameRAR", "Red Arcueid")]);

        // Assert
        Assert.Equal(file, patched);
    }

    [Fact]
    public void Insert_BytesAfterTheLastSection_StayTheSame()
    {
        // Arrange
        byte[] file = [.. File("A\r\nB\r\n/// End Of Text\r\n"), .. Trailer];

        // Act
        byte[] patched = IdList.Insert(file, [new("C", "D")]);

        // Assert
        Assert.Equal(Trailer, patched[^Trailer.Length..]);
    }

    [Fact]
    public void Insert_ListWithoutSectionEnd_AddsTheEntriesAtTheEnd()
    {
        // Arrange
        byte[] file = File("A\r\nB\r\n");

        // Act
        byte[] patched = IdList.Insert(file, [new("C", "D")]);

        // Assert
        Assert.Equal(File("A\r\nB\r\nC\r\nD\r\n"), patched);
    }

    private static byte[] File(string text) => [0xFF, 0xFE, .. Encoding.Unicode.GetBytes(text)];
}
