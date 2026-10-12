namespace BBTAG_IM.Core.Overlay;

public sealed class MenuTextureIds
{
    private readonly HashSet<ulong> _ids = [];

    public void Add(ulong id) => _ids.Add(id);

    public void Remove(ulong id) => _ids.Remove(id);

    public bool Contains(ulong id) => _ids.Contains(id);
}
