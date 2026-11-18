using BBTAG_IM.Core.Mods.Assets;
using BBTAG_IM.Core.Patching;

namespace BBTAG_IM.Core.Tests.Mods.Assets;

public class DataFolderReadingPatchTests
{
    private const int ImageSize = 0x40000;

    private static readonly (uint Rva, byte[] Original, byte[] Patched)[] SteamToPatched =
    [
        (0x325FF, [0x7E], [0xEB]),
        (0x32F45, [0x33, 0xFF], [0x90, 0x90]),
        (0x32F67, [0x47], [0x90]),
        (0x32F7A, [0x50], [0x57]),
        (0x33768, [0xB8, 0x01, 0x00, 0x00, 0x20, 0x0F, 0x4F], [0x8B, 0x74, 0x24, 0x04, 0x90, 0xEB, 0x01]),
        (0x3377A, [0x46], [0x90]),
        (0x3378D, [0x50], [0x56]),
    ];

    [Fact]
    public void StateOf_SteamExecutable_ReadsEncryptedAssets()
    {
        // Arrange
        byte[] image = Image(patched: false);

        // Act
        GameFileReading state = DataFolderReadingPatch.StateOf(Reader(image));

        // Assert
        Assert.Equal(GameFileReading.EncryptedAssets, state);
    }

    [Fact]
    public void StateOf_PatchedExecutable_ReadsTheDataFolder()
    {
        // Arrange
        byte[] image = Image(patched: true);

        // Act
        GameFileReading state = DataFolderReadingPatch.StateOf(Reader(image));

        // Assert
        Assert.Equal(GameFileReading.DataFolder, state);
    }

    [Fact]
    public void StateOf_OtherGameVersion_IsUnknown()
    {
        // Arrange
        byte[] image = new byte[ImageSize];

        // Act
        GameFileReading state = DataFolderReadingPatch.StateOf(Reader(image));

        // Assert
        Assert.Equal(GameFileReading.Unknown, state);
    }

    [Fact]
    public void StateOf_OnlySomeSpotsPatched_IsUnknown()
    {
        // Arrange
        byte[] image = Image(patched: false);
        image[0x325FF] = 0xEB;

        // Act
        GameFileReading state = DataFolderReadingPatch.StateOf(Reader(image));

        // Assert
        Assert.Equal(GameFileReading.Unknown, state);
    }

    [Fact]
    public void Writes_OnTheSteamExecutable_GiveThePatchedExecutable()
    {
        // Arrange
        byte[] image = Image(patched: false);

        // Act
        foreach (PatchWrite write in DataFolderReadingPatch.Writes)
            write.Bytes.CopyTo(image, (int)write.Rva);

        // Assert
        Assert.Equal(Image(patched: true), image);
    }

    [Fact]
    public void Writes_AnyWrite_HoldsNoAddressToRelocate()
    {
        // Act
        bool relocates = DataFolderReadingPatch.Writes.Any(write => write.AddressFields.Count > 0 || write.BranchFields.Count > 0);

        // Assert
        Assert.False(relocates);
    }

    private static byte[] Image(bool patched)
    {
        byte[] image = new byte[ImageSize];

        foreach (var (rva, original, patchedBytes) in SteamToPatched)
            (patched ? patchedBytes : original).CopyTo(image, (int)rva);

        return image;
    }

    private static Func<uint, int, byte[]> Reader(byte[] image) => (rva, length) => image.AsSpan((int)rva, length).ToArray();
}
