using BBTAG_IM.Core.Display;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Tests.Display;

public class WindowResolutionCodeTests
{
    private const uint CountSite = 0x006B35D2;
    private const uint BorderlessSite = 0x004210EC;
    private const uint WindowSite = 0x00421398;
    private const uint BorderlessReturn = 0x0042113E;
    private const uint WindowReturn = 0x004213E4;
    private const uint SavedReadAt = 0x00960195;
    private const uint SavedLimitSite = 0x009601A1;
    private const uint Block = 0x10000000;
    private const uint OverhaulJumpAt = 0x00421130;
    private const uint OverhaulCodeAt = 0x02337080;
    private const uint OverhaulBase = 0x02337106;
    private const uint OverhaulFirstSize = 0x023370EA;

    private static readonly byte[] StockCount = [0xBB, 0x03, 0x00, 0x00, 0x00, 0x8B, 0xF3, 0x8D, 0x7B, 0xFE, 0xEB, 0x20, 0x8D, 0x81, 0x4C, 0xB6, 0x04, 0x00];
    private static readonly byte[] ExtendedCount = [0xBB, 0x0C, 0x00, 0x00, 0x00, 0x8B, 0xF3, 0x8D, 0x7B, 0xF5, 0xEB, 0x20, 0x8D, 0x81, 0x4C, 0xB6, 0x04, 0x00];
    private static readonly byte[] BorderlessStart =
    [
        0x8B, 0x8F, 0x60, 0x01, 0x00, 0x00, 0x8B, 0x35, 0x50, 0xE3, 0xA5, 0x00,
        0xC7, 0x45, 0xDC, 0x00, 0x00, 0x00, 0x00, 0xC7, 0x45, 0xE0, 0x00, 0x00, 0x00, 0x00, 0xC7, 0x45, 0xE4, 0x00, 0x00, 0xA0, 0x44,
    ];
    private static readonly byte[] WindowStart =
    [
        0x8B, 0x8E, 0x60, 0x01, 0x00, 0x00,
        0xC7, 0x45, 0xDC, 0x00, 0x00, 0x00, 0x00, 0xC7, 0x45, 0xE0, 0x00, 0x00, 0x00, 0x00, 0xC7, 0x45, 0xE4, 0x00, 0x00, 0xA0, 0x44,
    ];
    private static readonly byte[] SavedRead =
    [
        0xFF, 0x71, 0x10, 0xFF, 0x71, 0x08, 0xFF, 0x15, 0x40, 0xE1, 0xA5, 0x00,
        0x83, 0xF8, 0x03, 0xB9, 0x02, 0x00, 0x00, 0x00, 0x0F, 0x43, 0xC1, 0xC3,
    ];
    private static readonly Resolution[] OverhaulResolutions =
    [
        new(640, 360), new(854, 480), new(960, 540), new(1024, 576),
        new(1280, 720), new(1366, 768), new(1600, 900), new(1920, 1080),
        new(2048, 1152), new(2560, 1440), new(3200, 1800), new(3840, 2160),
    ];
    private static readonly byte[] OverhaulCode =
    [
        0x55, 0x8D, 0x2D, .. BitConverter.GetBytes(OverhaulBase),
        0x8B, 0x44, 0xCD, 0xDC, 0x89, 0x45, 0xD4, 0x8B, 0x44, 0xCD, 0xE0, 0x89, 0x45, 0xD8, 0x5D,
    ];
    private static readonly byte[] Lookup =
    [
        0x8B, 0x04, 0xCD, .. BitConverter.GetBytes(Block + WindowResolutionCode.ListOffset),
        0x89, 0x45, 0xD4,
        0x8B, 0x04, 0xCD, .. BitConverter.GetBytes(Block + WindowResolutionCode.ListOffset + 4),
        0x89, 0x45, 0xD8,
    ];

    private static byte[] OverhaulSizeBytes() =>
        [.. OverhaulResolutions.SelectMany(resolution => BitConverter.GetBytes((float)resolution.Width).Concat(BitConverter.GetBytes((float)resolution.Height)))];

    public CodeImage CreateCodeImage(byte[] count) =>
        new([new CodeBlock(CountSite, count), new CodeBlock(BorderlessSite, BorderlessStart), new CodeBlock(WindowSite, WindowStart), new CodeBlock(SavedReadAt, SavedRead)]);

    public CodeImage CreateOverhaulImage(byte[] overhaulCode)
    {
        byte[] borderless = new byte[0x50];
        BorderlessStart.CopyTo(borderless, 0);
        borderless[0x44] = 0xE9;
        BitConverter.GetBytes(OverhaulCodeAt - (OverhaulJumpAt + 5)).CopyTo(borderless, 0x45);

        return new CodeImage(
        [
            new CodeBlock(CountSite, ExtendedCount),
            new CodeBlock(BorderlessSite, borderless),
            new CodeBlock(WindowSite, WindowStart),
            new CodeBlock(SavedReadAt, SavedRead),
            new CodeBlock(OverhaulCodeAt, overhaulCode),
            new CodeBlock(OverhaulFirstSize, OverhaulSizeBytes()),
        ]);
    }

    [Fact]
    public void Locate_GameCode_ReturnsTheCountTheTwoListSitesAndTheSavedLimit()
    {
        // Arrange
        CodeImage image = CreateCodeImage(StockCount);

        // Act
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);

        // Assert
        Assert.Equal(new WindowResolutionSites(CountSite, BorderlessSite, WindowSite, SavedLimitSite), sites);
    }

    [Fact]
    public void Locate_CodeWithoutTheWindowSite_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CountSite, StockCount), new CodeBlock(BorderlessSite, BorderlessStart), new CodeBlock(SavedReadAt, SavedRead)]);

        // Act
        Exception? exception = Record.Exception(() => WindowResolutionCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void Locate_CodeWithoutTheSavedLimit_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(CountSite, StockCount), new CodeBlock(BorderlessSite, BorderlessStart), new CodeBlock(WindowSite, WindowStart)]);

        // Act
        Exception? exception = Record.Exception(() => WindowResolutionCode.Locate(image));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void Count_StockCode_ReturnsThree()
    {
        // Arrange
        CodeImage image = CreateCodeImage(StockCount);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);

        // Act
        int count = WindowResolutionCode.Count(image, sites);

        // Assert
        Assert.Equal(WindowResolutionCode.StockCount, count);
    }

    [Fact]
    public void Count_ExtendedCode_ReturnsTwelve()
    {
        // Arrange
        CodeImage image = CreateCodeImage(ExtendedCount);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);

        // Act
        int count = WindowResolutionCode.Count(image, sites);

        // Assert
        Assert.Equal(WindowResolutionCode.Resolutions.Count, count);
    }

    [Fact]
    public void Resolutions_Always_KeepTheGameSizesFirstAndAddTheOthersFrom640x360To3840x2160()
    {
        // Arrange
        Resolution[] expected =
        [
            new(1280, 720), new(1600, 900), new(1920, 1080),
            new(640, 360), new(854, 480), new(960, 540), new(1024, 576), new(1366, 768),
            new(2048, 1152), new(2560, 1440), new(3200, 1800), new(3840, 2160),
        ];

        // Act
        IReadOnlyList<Resolution> resolutions = WindowResolutionCode.Resolutions;

        // Assert
        Assert.Equal(expected, resolutions);
    }

    [Fact]
    public void List_Always_StartsWithAnEmptyPairAndHoldsEachResolutionAsFloats()
    {
        // Arrange
        List<byte> expected = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        foreach (Resolution resolution in WindowResolutionCode.Resolutions)
        {
            expected.AddRange(BitConverter.GetBytes((float)resolution.Width));
            expected.AddRange(BitConverter.GetBytes((float)resolution.Height));
        }

        // Act
        byte[] list = WindowResolutionCode.List();

        // Assert
        Assert.Equal(expected, list);
    }

    [Fact]
    public void SavedLimitPatch_Always_AcceptsEveryPositionAndFallsBackToTheLast()
    {
        // Arrange
        byte[] expected = [0x83, 0xF8, 0x0C, 0xB9, 0x0B, 0x00, 0x00, 0x00];

        // Act
        byte[] patch = WindowResolutionCode.SavedLimitPatch();

        // Assert
        Assert.Equal(expected, patch);
    }

    [Fact]
    public void OverhaulSizes_OverhaulCode_ReadsItsTwelveSizesInItsOrder()
    {
        // Arrange
        CodeImage image = CreateOverhaulImage(OverhaulCode);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);

        // Act
        IReadOnlyList<Resolution> sizes = WindowResolutionCode.OverhaulSizes(image, sites);

        // Assert
        Assert.Equal(OverhaulResolutions, sizes);
    }

    [Fact]
    public void OverhaulList_OverhaulCode_ReturnsWhereItsFirstSizeIs()
    {
        // Arrange
        CodeImage image = CreateOverhaulImage(OverhaulCode);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);

        // Act
        uint list = WindowResolutionCode.OverhaulList(image, sites);

        // Assert
        Assert.Equal(OverhaulFirstSize, list);
    }

    [Fact]
    public void OverhaulList_NoJumpToItsList_ThrowsGameLayoutException()
    {
        // Arrange
        byte[] borderless = new byte[0x50];
        BorderlessStart.CopyTo(borderless, 0);
        CodeImage image = new([new CodeBlock(CountSite, ExtendedCount), new CodeBlock(BorderlessSite, borderless), new CodeBlock(WindowSite, WindowStart), new CodeBlock(SavedReadAt, SavedRead)]);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);

        // Act
        Exception? exception = Record.Exception(() => WindowResolutionCode.OverhaulList(image, sites));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void OverhaulList_JumpToOtherCode_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = CreateOverhaulImage(new byte[OverhaulCode.Length]);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);

        // Act
        Exception? exception = Record.Exception(() => WindowResolutionCode.OverhaulList(image, sites));

        // Assert
        Assert.IsType<GameLayoutException>(exception);
    }

    [Fact]
    public void CountPatch_Always_OffersTwelveChoicesAndKeepsTheThirdValueAtOne()
    {
        // Arrange
        byte[] expected = [0xBB, 0x0C, 0x00, 0x00, 0x00, 0x8B, 0xF3, 0x8D, 0x7B, 0xF5];

        // Act
        byte[] patch = WindowResolutionCode.CountPatch();

        // Assert
        Assert.Equal(expected, patch);
    }

    [Fact]
    public void BorderlessCode_SitesAndBlock_RepeatsTheMovedInstructionsReadsTheListAndReturnsPastTheStockList()
    {
        // Arrange
        CodeImage image = CreateCodeImage(StockCount);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);
        byte[] moved = BorderlessStart[..WindowResolutionCode.BorderlessSiteLength];
        uint jump = Block + WindowResolutionCode.BorderlessCodeOffset + (uint)(moved.Length + Lookup.Length);
        uint back = BorderlessReturn - (jump + WindowResolutionCode.JumpLength);

        // Act
        byte[] code = WindowResolutionCode.BorderlessCode(image, sites, Block);

        // Assert
        Assert.Equal([.. moved, .. Lookup, 0xE9, .. BitConverter.GetBytes(back)], code);
    }

    [Fact]
    public void WindowCode_SitesAndBlock_RepeatsTheMovedInstructionReadsTheListAndReturnsPastTheStockList()
    {
        // Arrange
        CodeImage image = CreateCodeImage(StockCount);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);
        byte[] moved = WindowStart[..WindowResolutionCode.WindowSiteLength];
        uint jump = Block + WindowResolutionCode.WindowCodeOffset + (uint)(moved.Length + Lookup.Length);
        uint back = WindowReturn - (jump + WindowResolutionCode.JumpLength);

        // Act
        byte[] code = WindowResolutionCode.WindowCode(image, sites, Block);

        // Assert
        Assert.Equal([.. moved, .. Lookup, 0xE9, .. BitConverter.GetBytes(back)], code);
    }

    [Fact]
    public void BorderlessJump_SitesAndBlock_JumpsToTheBorderlessCodeAndPadsTheMovedInstructions()
    {
        // Arrange
        CodeImage image = CreateCodeImage(StockCount);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);
        uint distance = Block + WindowResolutionCode.BorderlessCodeOffset - (BorderlessSite + WindowResolutionCode.JumpLength);

        // Act
        byte[] jump = WindowResolutionCode.BorderlessJump(sites, Block);

        // Assert
        Assert.Equal([0xE9, .. BitConverter.GetBytes(distance), 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90], jump);
    }

    [Fact]
    public void WindowJump_SitesAndBlock_JumpsToTheWindowCodeAndPadsTheMovedInstruction()
    {
        // Arrange
        CodeImage image = CreateCodeImage(StockCount);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);
        uint distance = Block + WindowResolutionCode.WindowCodeOffset - (WindowSite + WindowResolutionCode.JumpLength);

        // Act
        byte[] jump = WindowResolutionCode.WindowJump(sites, Block);

        // Assert
        Assert.Equal([0xE9, .. BitConverter.GetBytes(distance), 0x90], jump);
    }

    [Fact]
    public void Block_ListAndBothCodes_FitWithoutOverlapping()
    {
        // Arrange
        CodeImage image = CreateCodeImage(StockCount);
        WindowResolutionSites sites = WindowResolutionCode.Locate(image);

        // Act
        int listEnd = WindowResolutionCode.ListOffset + WindowResolutionCode.List().Length;
        int borderlessEnd = WindowResolutionCode.BorderlessCodeOffset + WindowResolutionCode.BorderlessCode(image, sites, Block).Length;
        int windowEnd = WindowResolutionCode.WindowCodeOffset + WindowResolutionCode.WindowCode(image, sites, Block).Length;

        // Assert
        Assert.True(listEnd <= WindowResolutionCode.BorderlessCodeOffset);
        Assert.True(borderlessEnd <= WindowResolutionCode.WindowCodeOffset);
        Assert.True(windowEnd <= WindowResolutionCode.BlockLength);
    }
}
