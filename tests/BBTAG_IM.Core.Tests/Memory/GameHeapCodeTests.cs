using System.Buffers.Binary;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Tests.Memory;

public class GameHeapCodeTests
{
    private const uint CodeStart = 0x00773184;
    private const uint OperatorNew = 0x00A031B5;
    private const int NodeFactoryLength = 0x48;

    [Fact]
    public void Locate_NodeFactoriesCallingTheSameAllocator_ReturnsIt()
    {
        // Arrange
        byte[] code = [.. NodeFactory(CodeStart, OperatorNew), .. NodeFactory(CodeStart + NodeFactoryLength, OperatorNew)];
        CodeImage image = new([new CodeBlock(CodeStart, code)]);

        // Act
        uint allocator = GameHeapCode.Locate(image);

        // Assert
        Assert.Equal(OperatorNew, allocator);
    }

    [Fact]
    public void Locate_NodeFactoriesCallingDifferentAllocators_ThrowsGameLayoutException()
    {
        // Arrange
        byte[] code = [.. NodeFactory(CodeStart, OperatorNew), .. NodeFactory(CodeStart + NodeFactoryLength, OperatorNew + 0x40)];
        CodeImage image = new([new CodeBlock(CodeStart, code)]);

        // Act
        Exception? exception = Record.Exception(() => GameHeapCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void Locate_NoNodeFactory_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CodeStart, [0x33, 0xC0, 0xC3])]);

        // Act
        Exception? exception = Record.Exception(() => GameHeapCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    private static byte[] NodeFactory(uint address, uint allocator)
    {
        byte[] code =
        [
            0x6A, 0x34, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x8B, 0xF0, 0x83, 0xC4, 0x04, 0x89, 0x75, 0xF0, 0xC7,
            0x45, 0xFC, 0x00, 0x00, 0x00, 0x00, 0x85, 0xF6, 0x74, 0x43, 0x8B, 0xCE, 0xE8, 0xDB, 0xDE, 0xC9,
            0xFF, 0x33, 0xC0, 0xC7, 0x46, 0x0C, 0x44, 0x0E, 0xAB, 0x00, 0x89, 0x46, 0x10, 0x89, 0x46, 0x14,
            0x89, 0x46, 0x18, 0x89, 0x46, 0x1C, 0x89, 0x46, 0x20, 0x89, 0x46, 0x24, 0x89, 0x46, 0x28, 0x89,
            0x46, 0x2C, 0x89, 0x46, 0x30, 0x8B, 0xC6, 0xC7,
        ];
        BinaryPrimitives.WriteInt32LittleEndian(code.AsSpan(3), (int)(allocator - (address + 7)));
        return code;
    }
}
