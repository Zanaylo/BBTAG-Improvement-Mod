namespace BBTAG_IM.Core.Overlay;

public readonly record struct MenuVertexRange(int First, int Count)
{
    public bool FitsIn(int vertexCount) => First + Count <= vertexCount;
}

public static class MenuDrawCommands
{
    private const uint IndicesPerTriangle = 3;

    public static bool CanDraw(ulong textureId, MenuTextureIds knownTextures, uint indexOffset, uint elementCount, int indexCount) =>
        knownTextures.Contains(textureId)
        && elementCount > 0
        && elementCount % IndicesPerTriangle == 0
        && (ulong)indexOffset + elementCount <= (ulong)indexCount;

    public static MenuVertexRange VertexRange(ReadOnlySpan<ushort> indices)
    {
        if (indices.IsEmpty)
            return default;

        var lowest = ushort.MaxValue;
        var highest = ushort.MinValue;

        foreach (var index in indices)
        {
            lowest = Math.Min(lowest, index);
            highest = Math.Max(highest, index);
        }

        return new MenuVertexRange(lowest, highest - lowest + 1);
    }
}
