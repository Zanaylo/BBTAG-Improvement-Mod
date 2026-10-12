using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Launch;

namespace BBTAG_IM.Payload;

public static unsafe class Exports
{
    [UnmanagedCallersOnly(EntryPoint = PayloadContract.StartExport, CallConvs = [typeof(CallConvStdcall)])]
    public static uint Start(char* session) => (uint)PayloadStartup.Run(Marshal.PtrToStringUni((nint)session) ?? string.Empty);
}
