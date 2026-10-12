using System.Runtime.InteropServices;
using BBTAG_IM.Core.Pe;

namespace BBTAG_IM.Payload.Native;

internal sealed unsafe class LoadedModule : IImageReader
{
    private const int HeaderPageSize = 0x1000;

    private LoadedModule(nint baseAddress, string path)
    {
        BaseAddress = baseAddress;
        Path = path;
        Headers = PeHeaders.Parse(new ReadOnlySpan<byte>((void*)baseAddress, HeaderPageSize));
    }

    public nint BaseAddress { get; }

    public string Path { get; }

    public PeHeaders Headers { get; }

    public static LoadedModule MainModule()
    {
        var module = Kernel32.GetModuleHandle(null);
        return new LoadedModule(module, Kernel32.ModulePath(module));
    }

    public nint AddressOf(uint rva) => BaseAddress + (nint)rva;

    public ushort ReadUInt16(uint rva) => *(ushort*)AddressOf(rva);

    public uint ReadUInt32(uint rva) => *(uint*)AddressOf(rva);

    public string ReadAsciiString(uint rva) => Marshal.PtrToStringAnsi(AddressOf(rva)) ?? string.Empty;
}
