using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.ImprovementMod;

public static unsafe class Exports
{
    [UnmanagedCallersOnly(EntryPoint = ModPlugin.StartExport, CallConvs = [typeof(CallConvStdcall)])]
    public static uint Start(char* modFolder, char* logFolder) =>
        ImprovementStartup.Run(Marshal.PtrToStringUni((nint)modFolder) ?? "", Marshal.PtrToStringUni((nint)logFolder) ?? "");
}
