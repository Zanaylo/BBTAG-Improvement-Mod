using BBTAG_IM.Core.Buttons;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Tests.Sprites;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Buttons;

public class ButtonIconArchiveTests
{
    private const string ModData = @"C:\Game\mods\Mod\data";
    private const string GameData = @"C:\Game\data";
    private const string Cache = @"C:\Game\mods\BBTAG-Improvement-Mod\cache";
    private const string Requested = @"C:\Game\data\gr\gr_johchu_cmn.pac";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Substitute_IconArchive_ServesThePatchedCopy()
    {
        // Arrange
        _fileSystemMock.FileExists(Path.Combine(GameData, "gr", "gr_johchu_cmn.pac")).Returns(true);
        _fileSystemMock.ReadAllBytes(Path.Combine(GameData, "gr", "gr_johchu_cmn.pac")).Returns(GameArchive());
        ButtonIconArchive archive = CreateButtonIconArchive();

        // Act
        string? served = archive.Substitute(Requested);

        // Assert
        Assert.Equal(Path.Combine(Cache, "gr_johchu_cmn.pac"), served);
        _fileSystemMock.Received(1).WriteAllBytes(Path.Combine(Cache, "gr_johchu_cmn.pac"), Arg.Any<byte[]>());
    }

    [Fact]
    public void Substitute_ModCopyOfTheArchive_PatchesTheModCopy()
    {
        // Arrange
        _fileSystemMock.FileExists(Path.Combine(ModData, "gr", "gr_johchu_cmn.pac")).Returns(true);
        _fileSystemMock.ReadAllBytes(Path.Combine(ModData, "gr", "gr_johchu_cmn.pac")).Returns(GameArchive());
        ButtonIconArchive archive = CreateButtonIconArchive();

        // Act
        archive.Substitute(Requested);

        // Assert
        _fileSystemMock.DidNotReceive().ReadAllBytes(Path.Combine(GameData, "gr", "gr_johchu_cmn.pac"));
    }

    [Fact]
    public void Substitute_SecondRequest_ReusesTheWrittenCopy()
    {
        // Arrange
        _fileSystemMock.FileExists(Path.Combine(GameData, "gr", "gr_johchu_cmn.pac")).Returns(true);
        _fileSystemMock.ReadAllBytes(Path.Combine(GameData, "gr", "gr_johchu_cmn.pac")).Returns(GameArchive());
        _fileSystemMock.FileExists(Path.Combine(Cache, "gr_johchu_cmn.pac")).Returns(true);
        ButtonIconArchive archive = CreateButtonIconArchive();
        archive.Substitute(Requested);

        // Act
        archive.Substitute(Requested);

        // Assert
        _fileSystemMock.Received(1).WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Theory]
    [InlineData(@"C:\Game\data\gr\gr_johchu_cmn2.pac")]
    [InlineData(@"C:\Game\data\etc\gr_johchu_cmn.pac")]
    [InlineData(null)]
    public void Substitute_OtherFile_IsNotServed(string? requested)
    {
        // Arrange
        ButtonIconArchive archive = CreateButtonIconArchive();

        // Act
        string? served = archive.Substitute(requested);

        // Assert
        Assert.Null(served);
    }

    [Fact]
    public void Substitute_ArchiveMissing_IsNotServed()
    {
        // Arrange
        ButtonIconArchive archive = CreateButtonIconArchive();

        // Act
        string? served = archive.Substitute(Requested);

        // Assert
        Assert.Null(served);
    }

    public ButtonIconArchive CreateButtonIconArchive() => new(_fileSystemMock, [ModData, GameData], Cache);

    private static byte[] GameArchive()
    {
        int width = ButtonIcons.CellSize * ButtonIcons.Columns;
        int height = 32 * ButtonIcons.CellSize;
        List<byte> runs = [];

        for (int left = width * height; left > 0; left -= 255)
            runs.AddRange([0, 0, 0, 0, (byte)Math.Min(255, left)]);

        byte[] atlas = ColorHipTests.Hip(width, height, [.. runs]);
        return LooseArchive.Write([new ArchiveFile("fhd_ads.pac", LooseArchive.Write([new ArchiveFile("icon.hip", atlas)]))]);
    }
}
