using System.Runtime.InteropServices;

namespace BBTAG_IM.Native;

[StructLayout(LayoutKind.Sequential)]
public struct FileOverlapped
{
    public nuint Internal;
    public nuint InternalHigh;
    public uint Offset;
    public uint OffsetHigh;
    public nint Event;
}
