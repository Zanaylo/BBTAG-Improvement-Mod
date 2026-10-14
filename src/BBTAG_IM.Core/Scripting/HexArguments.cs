namespace BBTAG_IM.Core.Scripting;

public sealed class HexArguments
{
    public static readonly HexArguments None = new(false, new HashSet<int>());

    public static readonly HexArguments All = new(true, new HashSet<int>());

    private readonly bool all;
    private readonly IReadOnlySet<int> indexes;

    private HexArguments(bool all, IReadOnlySet<int> indexes)
    {
        this.all = all;
        this.indexes = indexes;
    }

    public static HexArguments Only(IEnumerable<int> indexes) => new(false, indexes.ToHashSet());

    public bool Contains(int index) => all || indexes.Contains(index);
}
