using System.Runtime.InteropServices;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods.Assets;
using BBTAG_IM.Native;

namespace BBTAG_IM.Payload.Patching;

internal static class DataFolderReading
{
    public static void Apply(LoadedModule game, ILog log)
    {
        switch (DataFolderReadingPatch.StateOf((rva, length) => Read(game, rva, length)))
        {
            case GameFileReading.EncryptedAssets:
                Patch(game);
                log.Info("This BBTAG.exe decrypts the asset folder by itself. It now asks for the data folder instead, so mods and the DEV folder work, and the asset files are decrypted while the game reads them.");
                break;
            case GameFileReading.DataFolder:
                log.Info("This BBTAG.exe already reads the data folder.");
                break;
            default:
                log.Info("This BBTAG.exe version is not known, so it keeps reading game files its native way. Mod files are still handed to it under their asset names.");
                break;
        }
    }

    private static void Patch(LoadedModule game)
    {
        foreach (var write in DataFolderReadingPatch.Writes)
            ProtectedMemory.Write(game.AddressOf(write.Rva), write.Bytes);

        Kernel32.FlushInstructionCache(Kernel32.GetCurrentProcess(), 0, 0);
    }

    private static byte[] Read(LoadedModule game, uint rva, int length)
    {
        var bytes = new byte[length];
        Marshal.Copy(game.AddressOf(rva), bytes, 0, length);
        return bytes;
    }
}
