using System.Text;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Roster;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Roster;

public class RosterCodeTableTests
{
    private const uint CodeStart = 0x00674B20;
    private const uint Table = 0x00BD5E90;
    private const uint Strings = 0x00AB0000;

    [Fact]
    public void Read_TableOfCodes_ReturnsThemInIndexOrder()
    {
        // Arrange
        FakeGameMemory memory = TableWith("brg", "bjn", "bno");

        // Act
        IReadOnlyList<string> codes = RosterCodeTable.Read(Image(), memory);

        // Assert
        Assert.Equal(["brg", "bjn", "bno"], codes);
    }

    [Fact]
    public void Read_CodeWithoutTheRosterFill_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, [0x33, 0xC0, 0xC3])]);

        // Act
        Exception? exception = Record.Exception(() => RosterCodeTable.Read(image, TableWith("brg")));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    private static CodeImage Image() =>
        new([new CodeBlock(CodeStart, [0x8B, 0x0C, 0x9D, .. BitConverter.GetBytes(Table), 0x85, 0xC9, 0x0F, 0x84, 0x14, 0x01, 0x00, 0x00])]);

    private static FakeGameMemory TableWith(params string[] codes)
    {
        FakeGameMemory memory = new FakeGameMemory().Map(Table, 65 * 4).Map(Strings, 0x100);

        for (int index = 0; index < codes.Length; index++)
        {
            uint text = Strings + (uint)index * 0x10;
            memory.BytesAt(text, Encoding.ASCII.GetBytes(codes[index] + "\0")).UInt32At(Table + (uint)index * 4, text);
        }

        return memory;
    }
}
