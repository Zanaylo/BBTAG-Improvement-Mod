using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.Native;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod.Files;

internal static unsafe class FileSubstitutionHook
{
    private const string Kernel32Dll = "KERNEL32.dll";

    private static IReadOnlyList<FileSubstitution> s_substitutions = [];
    private static ILog s_log = null!;

    private static delegate* unmanaged[Stdcall]<char*, uint, uint, nint, uint, uint, nint, nint> s_kernelCreateFileW;
    private static delegate* unmanaged[Stdcall]<byte*, uint, uint, nint, uint, uint, nint, nint> s_createFileA;
    private static delegate* unmanaged[Stdcall]<char*, uint, uint, nint, uint, uint, nint, nint> s_createFileW;

    public static void Install(LoadedModule game, IReadOnlyList<FileSubstitution> substitutions, ILog log)
    {
        s_substitutions = substitutions;
        s_log = log;
        s_kernelCreateFileW = (delegate* unmanaged[Stdcall]<char*, uint, uint, nint, uint, uint, nint, nint>)
            Kernel32.GetProcAddress(Kernel32.GetModuleHandle(Kernel32Dll), "CreateFileW");

        s_createFileA = (delegate* unmanaged[Stdcall]<byte*, uint, uint, nint, uint, uint, nint, nint>)ImportHook.Replace(
            game, Kernel32Dll, "CreateFileA", (nint)(delegate* unmanaged[Stdcall]<byte*, uint, uint, nint, uint, uint, nint, nint>)&CreateFileA);
        s_createFileW = (delegate* unmanaged[Stdcall]<char*, uint, uint, nint, uint, uint, nint, nint>)ImportHook.Replace(
            game, Kernel32Dll, "CreateFileW", (nint)(delegate* unmanaged[Stdcall]<char*, uint, uint, nint, uint, uint, nint, nint>)&CreateFileW);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint CreateFileA(byte* fileName, uint access, uint share, nint security, uint disposition, uint flags, nint template)
    {
        if (Substitute(Marshal.PtrToStringAnsi((nint)fileName), access, disposition) is not { } substitute)
            return s_createFileA(fileName, access, share, security, disposition, flags, template);

        fixed (char* path = substitute)
            return s_kernelCreateFileW(path, access, share, security, disposition, flags, template);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint CreateFileW(char* fileName, uint access, uint share, nint security, uint disposition, uint flags, nint template)
    {
        if (Substitute(Marshal.PtrToStringUni((nint)fileName), access, disposition) is not { } substitute)
            return s_createFileW(fileName, access, share, security, disposition, flags, template);

        fixed (char* path = substitute)
            return s_kernelCreateFileW(path, access, share, security, disposition, flags, template);
    }

    private static string? Substitute(string? requested, uint access, uint disposition)
    {
        if (requested is null || !ModFileRedirector.ReadsExistingFile(access, disposition))
            return null;

        var substitution = s_substitutions.FirstOrDefault(candidate => requested.Contains(candidate.Marker, StringComparison.OrdinalIgnoreCase));

        if (substitution is null)
            return null;

        try
        {
            var substitute = substitution.Substitute(requested);

            if (substitute is not null)
                SafeLog.Info(s_log, substitution.Served(Path.GetFileName(requested)));

            return substitute;
        }
        catch (Exception exception)
        {
            SafeLog.Error(s_log, substitution.Failed(Path.GetFileName(requested), exception.Message));
            return null;
        }
    }
}
