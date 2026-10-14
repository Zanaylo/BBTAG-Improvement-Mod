using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class DevOverlayTests
{
    private const string Dev = @"D:\BBTAG\mods\DEV";
    private const string Key = @"data\char\char_brg_scr.pac";
    private const string LooseFolder = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr";
    private const string LooseScript = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.bin";
    private const string LooseSource = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.py";
    private const string Cache = @"D:\BBTAG\mods\DEV\.cache\data\char\char_brg_scr.pac";
    private const string Original = @"D:\BBTAG\data\char\char_brg_scr.pac";

    private static readonly DateTime Earlier = new(2026, 10, 13, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = Earlier.AddMinutes(5);

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Resolve_FileInTheDevFolder_ReturnsIt()
    {
        // Arrange
        _fileSystemMock.FileExists(Dev + @"\" + Key).Returns(true);
        DevOverlay overlay = CreateDevOverlay();

        // Act
        string? resolved = overlay.Resolve(Key);

        // Assert
        Assert.Equal(Dev + @"\" + Key, resolved);
    }

    [Fact]
    public void Resolve_LooseArchiveFolder_BuildsTheArchiveFromItAndTheOriginal()
    {
        // Arrange
        LooseFolderWith(Later);
        _fileSystemMock.FileExists(Original).Returns(true);
        _fileSystemMock.ReadAllBytes(Original).Returns(SpriteFiles.Fpac(("scr_brg.bin", [1]), ("scr_brgea.bin", [2])));
        DevOverlay overlay = CreateDevOverlay();

        // Act
        string? resolved = overlay.Resolve(Key);

        // Assert
        Assert.Equal(Cache, resolved);
        _fileSystemMock.Received(1).WriteAllBytes(Cache, Arg.Is<byte[]>(archive =>
            LooseArchive.Read(archive).Select(file => file.Name + ":" + file.Bytes[0]).SequenceEqual(new[] { "scr_brg.bin:9", "scr_brgea.bin:2" })));
    }

    [Fact]
    public void Resolve_CacheNewerThanEveryFile_ReusesIt()
    {
        // Arrange
        LooseFolderWith(Earlier);
        _fileSystemMock.FileExists(Cache).Returns(true);
        _fileSystemMock.LastWriteTimeUtc(Cache).Returns(Later);
        DevOverlay overlay = CreateDevOverlay();

        // Act
        string? resolved = overlay.Resolve(Key);

        // Assert
        Assert.Equal(Cache, resolved);
        _fileSystemMock.DidNotReceive().WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public void Resolve_NothingInTheDevFolder_ReturnsNull()
    {
        // Arrange
        DevOverlay overlay = CreateDevOverlay();

        // Act
        string? resolved = overlay.Resolve(Key);

        // Assert
        Assert.Null(resolved);
    }

    public DevOverlay CreateDevOverlay() => new(_fileSystemMock, Dev, key => key == Key ? Original : null);

    private void LooseFolderWith(DateTime written)
    {
        _fileSystemMock.DirectoryExists(LooseFolder).Returns(true);
        _fileSystemMock.EnumerateFiles(LooseFolder).Returns([LooseScript, LooseSource]);
        _fileSystemMock.ReadAllBytes(LooseScript).Returns([9]);
        _fileSystemMock.LastWriteTimeUtc(LooseScript).Returns(written);
        _fileSystemMock.LastWriteTimeUtc(Original).Returns(Earlier);
    }
}
