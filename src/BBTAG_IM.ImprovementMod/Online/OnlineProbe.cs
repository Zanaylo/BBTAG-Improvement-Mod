using System.Text;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Online;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Online;

internal sealed class OnlineProbe(GameView game, IGameMemory memory, GameAddresses addresses, LoadedModule gameModule, ILog log)
{
    private const int ConnectEveryFrames = 60;
    private const int ReportAfterMatchFrames = 300;
    private const uint SideFieldOffset = 0x2848;
    private const uint GameObjectLength = 0x60000;
    private const uint BattleObjectLength = 0x100000;
    private const int PageLength = 0x1000;
    private const int PageOverlap = 0x40;
    private const int LongestFindList = 24;

    private readonly P2PTraffic _traffic = new();

    private SteamSession? _steam;
    private int _framesWaited;
    private int _matchFrames;
    private bool _reported;

    public void OnPresent(nint device)
    {
        if (!Connect())
            return;

        if (game.State != GameState.Match)
        {
            _matchFrames = 0;
            _reported = false;
            return;
        }

        if (_reported)
            return;

        if (_matchFrames++ == 0)
            _traffic.Clear();

        if (_matchFrames < ReportAfterMatchFrames)
            return;

        _reported = true;
        var snapshot = _traffic.Take();

        if (snapshot.Remotes.Count == 0)
            return;

        var facts = $"Online probe: mode {(int)game.Mode}, side field {memory.ReadInt32(addresses.Game + SideFieldOffset)}, you {_steam!.OwnId}, "
            + $"the game sends to {string.Join(", ", snapshot.Remotes)} on channels {string.Join(", ", snapshot.Channels)}.";
        var steam = _steam;
        Task.Run(() => Report(facts, steam, snapshot));
    }

    private bool Connect()
    {
        if (_steam is not null)
            return true;

        if (++_framesWaited % ConnectEveryFrames != 0)
            return false;

        _steam = SteamSession.TryConnect();

        if (_steam is null)
            return false;

        P2PObserver.Install(_steam.Networking, _traffic);
        SafeLog.Info(log, "Steam networking found. Online matches are watched to learn where the game keeps the players.");
        return true;
    }

    private void Report(string facts, SteamSession steam, P2PSnapshot snapshot)
    {
        try
        {
            var lines = new StringBuilder(facts);
            var areas = Areas();
            AppendFinds(lines, areas, "your Steam ID", BitConverter.GetBytes(steam.OwnId));

            foreach (var remote in snapshot.Remotes)
                AppendFinds(lines, areas, $"Steam ID {remote}", BitConverter.GetBytes(remote));

            if (steam.PersonaName.Length > 0)
            {
                AppendFinds(lines, areas, "your name in UTF-8", Encoding.UTF8.GetBytes(steam.PersonaName));
                AppendFinds(lines, areas, "your name in UTF-16", Encoding.Unicode.GetBytes(steam.PersonaName));
            }

            SafeLog.Info(log, lines.ToString());
        }
        catch (Exception exception)
        {
            SafeLog.Error(log, $"The online probe stopped: {exception.Message}");
        }
    }

    private List<(uint Start, uint Length)> Areas()
    {
        var moduleBase = (uint)gameModule.BaseAddress;
        var sections = gameModule.Headers.Sections
            .Where(section => !section.IsExecutable)
            .Select(section => (moduleBase + section.VirtualAddress, section.MappedSize));
        var heap = MemoryMap.PrivateWritableRegions()
            .Select(region => ((uint)region.BaseAddress, (uint)region.RegionSize));

        return [.. sections, .. heap];
    }

    private void AppendFinds(StringBuilder lines, List<(uint Start, uint Length)> areas, string what, byte[] needle)
    {
        var finds = areas.SelectMany(area => Find(area.Start, area.Length, needle)).Distinct().Take(LongestFindList).Select(Describe).ToList();
        lines.Append($" {what}: ");
        lines.Append(finds.Count == 0 ? "not found." : string.Join(", ", finds) + ".");
    }

    private static IEnumerable<uint> Find(uint start, uint length, byte[] needle)
    {
        var page = new byte[PageLength + PageOverlap];

        for (var offset = 0u; offset < length; offset += PageLength)
        {
            var address = start + offset;
            var read = page.AsSpan(0, (int)Math.Min(page.Length, length - offset));

            if (!MemoryMap.TryRead(address, read) && !MemoryMap.TryRead(address, read = read[..Math.Min(PageLength, read.Length)]))
                continue;

            foreach (var found in ByteSearch.Offsets(read, needle).Where(found => found < PageLength))
                yield return address + (uint)found;
        }
    }

    private string Describe(uint address)
    {
        if (address - addresses.Game < GameObjectLength)
            return $"game+0x{address - addresses.Game:X}";

        if (address - addresses.Battle < BattleObjectLength)
            return $"battle+0x{address - addresses.Battle:X}";

        var moduleOffset = address - (uint)gameModule.BaseAddress;

        return moduleOffset < gameModule.Headers.SizeOfImage ? $"BBTAG.exe+0x{moduleOffset:X}" : $"0x{address:X8}";
    }
}
