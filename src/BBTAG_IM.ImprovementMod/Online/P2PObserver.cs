using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Online;
using BBTAG_IM.Native.Hooks;

namespace BBTAG_IM.ImprovementMod.Online;

internal static unsafe class P2PObserver
{
    private const int SendP2PPacketSlot = 0;

    private static delegate* unmanaged[Thiscall]<nint, ulong, void*, uint, int, int, byte> s_sendP2PPacket;
    private static P2PTraffic? s_traffic;

    public static void Install(nint networking, P2PTraffic traffic)
    {
        if (s_traffic is not null)
            return;

        s_traffic = traffic;
        s_sendP2PPacket = (delegate* unmanaged[Thiscall]<nint, ulong, void*, uint, int, int, byte>)VirtualTable.Read(networking, SendP2PPacketSlot);
        VirtualTable.Replace(networking, SendP2PPacketSlot, (nint)(delegate* unmanaged[Thiscall]<nint, ulong, void*, uint, int, int, byte>)&SendP2PPacket);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvThiscall)])]
    private static byte SendP2PPacket(nint networking, ulong remote, void* data, uint length, int sendType, int channel)
    {
        s_traffic?.Sent(remote, channel);
        return s_sendP2PPacket(networking, remote, data, length, sendType, channel);
    }
}
