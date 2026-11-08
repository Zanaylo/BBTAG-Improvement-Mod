namespace BBTAG_IM.Core.Memory;

public static class GameHeapCode
{
    private const string Name = "Game memory allocator";
    private const uint AllocatorCallOffset = 2;

    private static readonly BytePattern NodeFactory = BytePattern.Parse(
        "6A 34 E8 ?? ?? ?? ?? 8B F0 83 C4 04 89 75 F0 C7 45 FC 00 00 00 00 85 F6 74 ?? 8B CE E8 ?? ?? ?? ?? 33 C0 C7 46 0C ?? ?? ?? ?? " +
        "89 46 10 89 46 14 89 46 18 89 46 1C 89 46 20 89 46 24 89 46 28 89 46 2C 89 46 30 8B C6 C7");

    public static uint Locate(CodeImage image)
    {
        var allocators = image.FindAll(NodeFactory).Select(site => image.CallTarget(site + AllocatorCallOffset, Name)).Distinct().ToList();

        return allocators.Count switch
        {
            1 => allocators[0],
            0 => throw new GameLayoutException($"{Name} was not found in the game code. {GameLayoutException.UnsupportedVersion}"),
            _ => throw new GameLayoutException($"{Name} was found {allocators.Count} times in the game code. {GameLayoutException.UnsupportedVersion}"),
        };
    }
}
