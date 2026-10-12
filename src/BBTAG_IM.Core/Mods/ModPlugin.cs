using BBTAG_IM.Core.Pe;

namespace BBTAG_IM.Core.Mods;

public static class ModPlugin
{
    public const string StartExport = "BBTAG_IM_ModStart";
    public const uint Started = 0;
    public const uint Failed = 1;

    public static uint? FindStart(FileImage dll) => ExportTable.FindFunctionRva(dll, dll.Headers, StartExport);

    public static bool IsPlugin(string dllPath)
    {
        try
        {
            return FindStart(FileImage.Load(dllPath)) is not null;
        }
        catch (Exception exception) when (exception is InvalidPeImageException or IOException)
        {
            return false;
        }
    }
}
