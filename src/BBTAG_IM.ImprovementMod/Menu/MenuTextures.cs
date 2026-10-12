using BBTAG_IM.Core.Overlay;
using BBTAG_IM.ImprovementMod.Graphics;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed unsafe class MenuTextures
{
    private const uint DynamicUsage = 0x200;
    private const int ArgbFormat = 21;

    private readonly HashSet<nint> _created = [];

    private Direct3DDevice? _device;

    public event Action? Released;

    public MenuTextureIds Ids { get; } = new();

    public void Use(Direct3DDevice device) => _device = device;

    public nint Create(int width, int height, ReadOnlySpan<uint> pixels)
    {
        if (_device is not { } device || pixels.Length < width * height)
            return 0;

        var texture = device.CreateTexture((uint)width, (uint)height, DynamicUsage, ArgbFormat);

        if (texture == 0)
            return 0;

        if (!Upload(texture, width, height, pixels))
        {
            Direct3DDevice.Release(texture);
            return 0;
        }

        _created.Add(texture);
        Ids.Add((ulong)texture);
        return texture;
    }

    public void Release(nint texture)
    {
        if (!_created.Remove(texture))
            return;

        Ids.Remove((ulong)texture);
        Direct3DDevice.Release(texture);
    }

    public void ReleaseAll()
    {
        foreach (var texture in _created)
        {
            Ids.Remove((ulong)texture);
            Direct3DDevice.Release(texture);
        }

        _created.Clear();
        Released?.Invoke();
    }

    private static bool Upload(nint texture, int width, int height, ReadOnlySpan<uint> pixels)
    {
        var locked = Direct3DDevice.LockTexture(texture);

        if (locked.Bits == 0)
            return false;

        for (var row = 0; row < height; row++)
            pixels.Slice(row * width, width).CopyTo(new Span<uint>((void*)(locked.Bits + row * locked.Pitch), width));

        Direct3DDevice.UnlockTexture(texture);
        return true;
    }
}
