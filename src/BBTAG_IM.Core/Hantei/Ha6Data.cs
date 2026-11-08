namespace BBTAG_IM.Core.Hantei;

public sealed record Ha6Box(int X1, int Y1, int X2, int Y2);

public sealed record Ha6Layer(int Index, bool UsesPat, int Sprite, int X, int Y);

public sealed record Ha6Effect(int Type, int Number, IReadOnlyList<int> Parameters);

public sealed record Ha6Pattern(int Id, string Name, IReadOnlyList<Ha6Frame> Frames, string Code = "");

public sealed class Ha6Frame
{
    private static readonly IReadOnlyDictionary<string, int[]> NoChunks = new Dictionary<string, int[]>();
    private static readonly IReadOnlyDictionary<int, Ha6Box> NoBoxes = new Dictionary<int, Ha6Box>();

    public static Ha6Frame Empty { get; } = new();

    public int Duration { get; init; } = 1;

    public IReadOnlyList<Ha6Layer> Layers { get; init; } = [];

    public IReadOnlyDictionary<int, Ha6Box> Boxes { get; init; } = NoBoxes;

    public IReadOnlyDictionary<int, Ha6Box> AttackBoxes { get; init; } = NoBoxes;

    public IReadOnlyDictionary<string, int[]> Animation { get; init; } = NoChunks;

    public IReadOnlyDictionary<string, int[]> State { get; init; } = NoChunks;

    public IReadOnlyDictionary<string, int[]> Attack { get; init; } = NoChunks;

    public IReadOnlyList<Ha6Effect> Effects { get; init; } = [];
}
