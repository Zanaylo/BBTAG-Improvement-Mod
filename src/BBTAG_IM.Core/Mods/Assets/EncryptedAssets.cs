using System.Security.Cryptography;
using System.Text;

namespace BBTAG_IM.Core.Mods.Assets;

public static class EncryptedAssets
{
    public const string FolderName = "asset";

    private const char HashedSeparator = '/';
    private const int KeyStartByte = 7;
    private const int AssetNameLength = 32;
    private const string DataFolderPrefix = @"data\";
    private const string AssetFolderPrefix = FolderName + @"\";

    private static ReadOnlySpan<byte> Key =>
    [
        0xF5, 0x5C, 0x84, 0x2A, 0xAD, 0x61, 0x54, 0xE7, 0x0A, 0xFC, 0x99, 0x6B, 0xD5, 0xA4, 0xD3, 0xD8,
        0x48, 0x26, 0x69, 0xCB, 0x07, 0x42, 0x13, 0x5E, 0x10, 0x23, 0xD2, 0x6D, 0x36, 0xC7, 0xC1, 0x66,
        0xDF, 0xA1, 0xAD, 0xF1, 0x44, 0x44, 0x7E, 0xC9, 0x8E, 0x24, 0x99,
    ];

    public static string NameOf(string key) =>
        Md5Hex(key.Replace(GamePathKey.Separator, HashedSeparator).ToLowerInvariant());

    public static bool IsInDataFolder(string key) => key.StartsWith(DataFolderPrefix, StringComparison.OrdinalIgnoreCase);

    public static string? AssetNameIn(string key)
    {
        if (!key.StartsWith(AssetFolderPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var name = key[AssetFolderPrefix.Length..];
        return IsAssetName(name) ? name : null;
    }

    public static bool IsAssetName(string fileName) => fileName.Length == AssetNameLength && fileName.All(char.IsAsciiHexDigit);

    public static int KeyStartOf(string assetName) =>
        MD5.HashData(Encoding.ASCII.GetBytes(assetName.ToLowerInvariant()))[KeyStartByte] % Key.Length;

    public static void Decrypt(Span<byte> bytes, long fileOffset, int keyStart)
    {
        var key = Key;
        var position = (int)((keyStart + fileOffset % key.Length) % key.Length);

        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] ^= key[position];
            position = position + 1 == key.Length ? 0 : position + 1;
        }
    }

    private static string Md5Hex(string text) => Convert.ToHexStringLower(MD5.HashData(Encoding.ASCII.GetBytes(text)));
}
