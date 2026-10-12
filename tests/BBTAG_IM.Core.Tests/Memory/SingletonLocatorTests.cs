using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Memory;

public class SingletonLocatorTests
{
    private const uint CodeAddress = 0x401000;
    private const int CodeSize = 0x100;
    private const uint SiteOffset = 0x10;
    private const uint GetterOffset = 0x80;
    private const uint ObjectAddress = 0x00E59100;
    private const string SiteTail = "C7 80 18 01 00 00 17 00 00 00";

    private static readonly SingletonPattern Pattern = new("Game state", BytePattern.Parse("?? ?? ?? ?? ?? " + SiteTail), 0);

    public SingletonLocator CreateSingletonLocator(byte[] code) => new(new CodeImage([new CodeBlock(CodeAddress, code)]));

    [Fact]
    public void Locate_CallToALazySingletonGetter_ReturnsTheObjectAddress()
    {
        // Arrange
        byte[] code = CreateGameCode().ToArray();
        SingletonLocator locator = CreateSingletonLocator(code);

        // Act
        uint address = locator.Locate(Pattern);

        // Assert
        Assert.Equal(ObjectAddress, address);
    }

    [Fact]
    public void Locate_GetterLoadingTheAddressIntoEcx_ReturnsTheObjectAddress()
    {
        // Arrange
        byte[] code = CreateGameCode().ToArray();
        code[GetterOffset + GameCode.GetterAddressOffset - 1] = GameCode.MoveImmediateToEcx;
        SingletonLocator locator = CreateSingletonLocator(code);

        // Act
        uint address = locator.Locate(Pattern);

        // Assert
        Assert.Equal(ObjectAddress, address);
    }

    [Fact]
    public void Locate_SiteDoesNotStartWithACall_ThrowsGameLayoutException()
    {
        // Arrange
        byte[] code = CreateGameCode().ToArray();
        code[SiteOffset] = 0xE9;
        SingletonLocator locator = CreateSingletonLocator(code);

        // Act
        Exception? exception = Record.Exception(() => locator.Locate(Pattern));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Game state does not call its getter. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void Locate_GetterReturnsBeforeLoadingAnAddress_ThrowsGameLayoutException()
    {
        // Arrange
        byte[] code = CreateGameCode().ToArray();
        Array.Fill(code, GameCode.Return, (int)GetterOffset, GameCode.GetterLength);
        SingletonLocator locator = CreateSingletonLocator(code);

        // Act
        Exception? exception = Record.Exception(() => locator.Locate(Pattern));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("The Game state getter holds no object address. This game version is not supported.", layoutException.Message);
    }

    private static GameCode CreateGameCode() =>
        new GameCode(CodeSize)
            .CallAt(SiteOffset, GetterOffset)
            .Bytes(SiteOffset + 5, SiteTail)
            .SingletonGetterAt(GetterOffset, ObjectAddress);
}
