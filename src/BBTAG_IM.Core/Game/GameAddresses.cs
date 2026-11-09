using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public sealed record GameAddresses(uint Game, uint Battle, uint Characters, uint Cameras)
{
    private static readonly SingletonPattern GamePattern = new(
        "Game state",
        BytePattern.Parse("E8 ?? ?? ?? ?? C7 80 18 01 00 00 ?? 00 00 00"),
        CallOffset: 0,
        SiteChoice.MostCalled);

    private static readonly SingletonPattern BattlePattern = new(
        "Battle",
        BytePattern.Parse("8D 45 A0 6A 0F 50 6A 05 E8 ?? ?? ?? ?? 83 C4 10 E8"),
        CallOffset: 16);

    private static readonly SingletonPattern CharactersPattern = new(
        "Character list",
        BytePattern.Parse("56 68 00 05 00 00 6A 00 6A 00 8B F1 E8 ?? ?? ?? ?? 81 A6 50 01 00 00 FF F0 FF FF E8"),
        CallOffset: 27);

    private static readonly SingletonPattern CamerasPattern = new(
        "Camera list",
        BytePattern.Parse("C7 45 98 00 00 00 00 E8 ?? ?? ?? ?? 8D 4D 98 51 8B C8 E8"),
        CallOffset: 7);

    public static GameAddresses Locate(CodeImage image)
    {
        var locator = new SingletonLocator(image);

        return new GameAddresses(
            locator.Locate(GamePattern),
            locator.Locate(BattlePattern),
            locator.Locate(CharactersPattern),
            locator.Locate(CamerasPattern));
    }
}
