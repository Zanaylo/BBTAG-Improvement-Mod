using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Display;

public static class WindowResolutionCode
{
    public const int ListOffset = 0;
    public const int BorderlessCodeOffset = 112;
    public const int WindowCodeOffset = 160;
    public const int BlockLength = 192;
    public const int JumpLength = 5;
    public const int BorderlessSiteLength = 12;
    public const int WindowSiteLength = 6;
    public const int StockCount = 3;
    public const int ExtendedCount = 12;

    public static readonly IReadOnlyList<Resolution> Resolutions =
    [
        new(1280, 720), new(1600, 900), new(1920, 1080),
        new(640, 360), new(854, 480), new(960, 540), new(1024, 576), new(1366, 768),
        new(2048, 1152), new(2560, 1440), new(3200, 1800), new(3840, 2160),
    ];

    private const string CountName = "Window resolution count";
    private const string BorderlessName = "Borderless resolution list";
    private const string WindowName = "Window resolution list";
    private const int CountOffset = 1;
    private const int StockListLength = 70;
    private const int PairLength = 2 * sizeof(float);
    private const byte JumpNear = 0xE9;
    private const byte Nop = 0x90;
    private const byte LastValueOffset = unchecked((byte)(1 - ExtendedCount));

    private static readonly BytePattern CountSite = BytePattern.Parse("BB ?? 00 00 00 8B F3 8D 7B ?? EB 20 8D 81");
    private static readonly BytePattern BorderlessSite =
        BytePattern.Parse("8B 8F 60 01 00 00 8B 35 ?? ?? ?? ?? C7 45 DC 00 00 00 00 C7 45 E0 00 00 00 00 C7 45 E4 00 00 A0 44");
    private static readonly BytePattern WindowSite =
        BytePattern.Parse("8B 8E 60 01 00 00 C7 45 DC 00 00 00 00 C7 45 E0 00 00 00 00 C7 45 E4 00 00 A0 44");
    private static readonly byte[] LoadWidth = [0x8B, 0x04, 0xCD];
    private static readonly byte[] StoreWidth = [0x89, 0x45, 0xD4];
    private static readonly byte[] LoadHeight = [0x8B, 0x04, 0xCD];
    private static readonly byte[] StoreHeight = [0x89, 0x45, 0xD8];
    private static readonly byte[] EmptyPair = new byte[PairLength];

    public static WindowResolutionSites Locate(CodeImage image) =>
        new(image.FindSingle(CountSite, CountName), image.FindSingle(BorderlessSite, BorderlessName), image.FindSingle(WindowSite, WindowName));

    public static int Count(CodeImage image, WindowResolutionSites sites) => image.Slice(sites.Count + CountOffset, sizeof(byte))[0];

    public static byte[] List() =>
        [.. EmptyPair, .. Resolutions.SelectMany(resolution => Pair(resolution.Width, resolution.Height))];

    public static byte[] CountPatch() => [0xBB, ExtendedCount, 0x00, 0x00, 0x00, 0x8B, 0xF3, 0x8D, 0x7B, LastValueOffset];

    public static byte[] BorderlessCode(CodeImage image, WindowResolutionSites sites, uint block) =>
        Code(image, sites.Borderless, BorderlessSiteLength, block, BorderlessCodeOffset);

    public static byte[] WindowCode(CodeImage image, WindowResolutionSites sites, uint block) =>
        Code(image, sites.Window, WindowSiteLength, block, WindowCodeOffset);

    public static byte[] BorderlessJump(WindowResolutionSites sites, uint block) =>
        Jump(sites.Borderless, BorderlessSiteLength, block + BorderlessCodeOffset);

    public static byte[] WindowJump(WindowResolutionSites sites, uint block) =>
        Jump(sites.Window, WindowSiteLength, block + WindowCodeOffset);

    private static byte[] Code(CodeImage image, uint site, int siteLength, uint block, int codeOffset)
    {
        var moved = image.Slice(site, siteLength).ToArray();
        var list = block + ListOffset;
        byte[] lookup =
        [
            .. moved,
            .. LoadWidth, .. Address(list),
            .. StoreWidth,
            .. LoadHeight, .. Address(list + sizeof(float)),
            .. StoreHeight,
        ];
        var jump = block + (uint)codeOffset + (uint)lookup.Length;
        var back = site + (uint)siteLength + StockListLength;

        return [.. lookup, JumpNear, .. Address(back - (jump + JumpLength))];
    }

    private static byte[] Jump(uint site, int siteLength, uint target) =>
        [JumpNear, .. Address(target - (site + JumpLength)), .. Enumerable.Repeat(Nop, siteLength - JumpLength)];

    private static byte[] Pair(int width, int height) => [.. BitConverter.GetBytes((float)width), .. BitConverter.GetBytes((float)height)];

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
