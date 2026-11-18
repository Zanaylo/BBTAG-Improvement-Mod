using BBTAG_IM.Core.Patching;

namespace BBTAG_IM.Core.Mods.Assets;

public enum GameFileReading
{
    EncryptedAssets,
    DataFolder,
    Unknown,
}

public static class DataFolderReadingPatch
{
    private static readonly PatchSpot[] Spots =
    [
        new(0x325FF, [0x7E], [0xEB]),
        new(0x32F45, [0x33, 0xFF], [0x90, 0x90]),
        new(0x32F67, [0x47], [0x90]),
        new(0x32F7A, [0x50], [0x57]),
        new(0x33768, [0xB8, 0x01, 0x00, 0x00, 0x20, 0x0F, 0x4F], [0x8B, 0x74, 0x24, 0x04, 0x90, 0xEB, 0x01]),
        new(0x3377A, [0x46], [0x90]),
        new(0x3378D, [0x50], [0x56]),
    ];

    public static IReadOnlyList<PatchWrite> Writes { get; } = [.. Spots.Select(spot => new PatchWrite(spot.Rva, spot.Patched, [], []))];

    public static GameFileReading StateOf(Func<uint, int, byte[]> read)
    {
        if (Spots.All(spot => read(spot.Rva, spot.Original.Length).AsSpan().SequenceEqual(spot.Original)))
            return GameFileReading.EncryptedAssets;

        return Spots.All(spot => read(spot.Rva, spot.Patched.Length).AsSpan().SequenceEqual(spot.Patched))
            ? GameFileReading.DataFolder
            : GameFileReading.Unknown;
    }

    private sealed record PatchSpot(uint Rva, byte[] Original, byte[] Patched);
}
