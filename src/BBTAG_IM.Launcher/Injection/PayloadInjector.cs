using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Launcher.Native;

namespace BBTAG_IM.Launcher.Injection;

internal sealed class PayloadInjector(string payloadPath)
{
    private static readonly TimeSpan LoadTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(2);

    private readonly uint _startRva = FindStartExport(payloadPath);

    public PayloadStatus Inject(RemoteProcess process, LaunchSession session)
    {
        var module = LoadPayload(process);

        using var sessionText = process.WriteString(session.Serialize());
        var timeout = session.WaitForDebugger ? StartTimeout + DebuggerWait.Limit : StartTimeout;
        return (PayloadStatus)process.RunThread((nint)(module + _startRva), sessionText.Address, timeout);
    }

    private uint LoadPayload(RemoteProcess process)
    {
        var loadLibrary = NativeMethods.GetProcAddress(NativeMethods.GetModuleHandle("kernel32.dll"), "LoadLibraryW");

        using var path = process.WriteString(payloadPath);
        var module = process.RunThread(loadLibrary, path.Address, LoadTimeout);

        if (module == 0)
            throw new LaunchFailedException($"The game could not load {payloadPath}.");

        return module;
    }

    private static uint FindStartExport(string payloadPath)
    {
        if (!File.Exists(payloadPath))
            throw new LaunchFailedException($"{PayloadContract.DllFileName} is missing. Use every file of the same release.");

        var image = FileImage.Load(payloadPath);
        return ExportTable.FindFunctionRva(image, image.Headers, PayloadContract.StartExport)
            ?? throw new LaunchFailedException($"{payloadPath} is not a BBTAG_IM payload.");
    }
}
