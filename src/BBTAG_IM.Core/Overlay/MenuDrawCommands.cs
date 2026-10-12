namespace BBTAG_IM.Core.Overlay;

public static class MenuDrawCommands
{
    private const uint IndicesPerTriangle = 3;

    public static bool CanDraw(ulong textureId, ulong fontTexture, uint indexOffset, uint elementCount, int indexCount) =>
        textureId == fontTexture
        && elementCount > 0
        && elementCount % IndicesPerTriangle == 0
        && (ulong)indexOffset + elementCount <= (ulong)indexCount;
}
