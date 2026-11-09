using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Memory;

public class CodeImageTests
{
    private const uint FirstBlock = 0x401000;
    private const uint SecondBlock = 0x900000;
    private const int CallingCodeSize = 0x100;
    private const uint MostCalledFunction = 0x80;
    private const uint OtherFunction = 0xC0;
    private const string StoreAfterCall = "C7 80 18 01 00 00 17 00 00 00";

    private static readonly BytePattern CallThenStore = BytePattern.Parse("E8 ?? ?? ?? ?? C7 80 18 01 00 00 ?? 00 00 00");

    public CodeImage CreateCodeImage() =>
        new(
        [
            new CodeBlock(FirstBlock, [0x55, 0x8B, 0xEC, 0xC3]),
            new CodeBlock(SecondBlock, [0x90, 0x55, 0x8B, 0xEC]),
        ]);

    [Fact]
    public void FindAll_PatternInBothBlocks_ReturnsEveryAddress()
    {
        // Arrange
        CodeImage image = CreateCodeImage();

        // Act
        IReadOnlyList<uint> matches = image.FindAll(BytePattern.Parse("55 8B EC"));

        // Assert
        Assert.Equal([FirstBlock, SecondBlock + 1], matches);
    }

    [Fact]
    public void FindSingle_PatternInOneBlock_ReturnsItsAddress()
    {
        // Arrange
        CodeImage image = CreateCodeImage();

        // Act
        uint address = image.FindSingle(BytePattern.Parse("EC C3"), "Return");

        // Assert
        Assert.Equal(FirstBlock + 2, address);
    }

    [Fact]
    public void FindSingle_PatternMissing_ThrowsGameLayoutExceptionNamingIt()
    {
        // Arrange
        CodeImage image = CreateCodeImage();

        // Act
        Exception? exception = Record.Exception(() => image.FindSingle(BytePattern.Parse("CC CC"), "Camera"));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Camera was not found in the game code. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void FindSingle_PatternInTwoBlocks_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = CreateCodeImage();

        // Act
        Exception? exception = Record.Exception(() => image.FindSingle(BytePattern.Parse("55 8B EC"), "Frame"));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Frame was found 2 times in the game code. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void Slice_AddressInsideABlock_ReturnsTheBytesFromThere()
    {
        // Arrange
        CodeImage image = CreateCodeImage();

        // Act
        byte[] bytes = image.Slice(SecondBlock + 1, 3).ToArray();

        // Assert
        Assert.Equal([0x55, 0x8B, 0xEC], bytes);
    }

    [Fact]
    public void Slice_RangeRunsPastTheBlock_ReturnsWhatTheBlockHolds()
    {
        // Arrange
        CodeImage image = CreateCodeImage();

        // Act
        byte[] bytes = image.Slice(FirstBlock + 3, 16).ToArray();

        // Assert
        Assert.Equal([0xC3], bytes);
    }

    [Fact]
    public void Slice_AddressOutsideEveryBlock_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = CreateCodeImage();

        // Act
        Exception? exception = Record.Exception(() => image.Slice(0x500000, 4).ToArray());

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("0x00500000 is outside the game code.", layoutException.Message);
    }

    [Fact]
    public void CallTarget_CallInstruction_ReturnsTheAddressItCalls()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(FirstBlock, [0x90, 0xE8, 0x0A, 0x00, 0x00, 0x00, 0xC3])]);

        // Act
        uint target = image.CallTarget(FirstBlock + 1, "Fate");

        // Assert
        Assert.Equal(FirstBlock + 1 + 5 + 0x0A, target);
    }

    [Fact]
    public void CallTarget_InstructionThatIsNoCall_ThrowsGameLayoutExceptionNamingIt()
    {
        // Arrange
        CodeImage image = CreateCodeImage();

        // Act
        Exception? exception = Record.Exception(() => image.CallTarget(FirstBlock, "Fate"));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Fate does not call its getter. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void MostCalledTarget_MostPlacesCallOneFunction_ReturnsThatFunction()
    {
        // Arrange
        CodeImage image = CreateCallingImage(MostCalledFunction, OtherFunction, MostCalledFunction);

        // Act
        uint target = image.MostCalledTarget(CallThenStore, 0, "Game state");

        // Assert
        Assert.Equal(FirstBlock + MostCalledFunction, target);
    }

    [Fact]
    public void MostCalledTarget_PatternMissing_ThrowsGameLayoutExceptionNamingIt()
    {
        // Arrange
        CodeImage image = CreateCodeImage();

        // Act
        Exception? exception = Record.Exception(() => image.MostCalledTarget(CallThenStore, 0, "Game state"));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Game state was not found in the game code. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void MostCalledTarget_FunctionsCalledEquallyOften_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = CreateCallingImage(MostCalledFunction, OtherFunction);

        // Act
        Exception? exception = Record.Exception(() => image.MostCalledTarget(CallThenStore, 0, "Game state"));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Game state calls different functions equally often in the game code. This game version is not supported.", layoutException.Message);
    }

    private static CodeImage CreateCallingImage(params uint[] targets)
    {
        var code = new GameCode(CallingCodeSize);

        for (var place = 0; place < targets.Length; place++)
        {
            var offset = (uint)(place * 0x10);
            code.CallAt(offset, targets[place]).Bytes(offset + 5, StoreAfterCall);
        }

        return new CodeImage([new CodeBlock(FirstBlock, code.ToArray())]);
    }
}
