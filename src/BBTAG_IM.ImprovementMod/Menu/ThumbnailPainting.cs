using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.ImprovementMod.Menu;

internal readonly record struct ThumbnailKey(ImplPalette Palette, int Width, int Height);

internal sealed class ThumbnailPainting
{
    private const int MostPending = 24;
    private const int StaleAfterFrames = 120;

    private readonly ConcurrentDictionary<ThumbnailKey, uint[]> _painted = [];
    private readonly Dictionary<ThumbnailKey, int> _wantedAt = [];

    public bool TryTake(ThumbnailKey key, [NotNullWhen(true)] out uint[]? pixels)
    {
        if (!_painted.TryRemove(key, out pixels))
            return false;

        _wantedAt.Remove(key);
        return true;
    }

    public void Want(ThumbnailKey key, IndexedSprite sprite, int frame)
    {
        if (_wantedAt.ContainsKey(key))
        {
            _wantedAt[key] = frame;
            return;
        }

        if (_wantedAt.Count >= MostPending && !ForgetStale(frame))
            return;

        _wantedAt[key] = frame;
        Task.Run(() => _painted[key] = Paint(sprite, key));
    }

    private bool ForgetStale(int frame)
    {
        var stale = _wantedAt
            .Where(wanted => frame - wanted.Value > StaleAfterFrames && _painted.ContainsKey(wanted.Key))
            .Select(wanted => wanted.Key)
            .ToArray();

        foreach (var key in stale)
        {
            _wantedAt.Remove(key);
            _painted.TryRemove(key, out _);
        }

        return stale.Length > 0;
    }

    private static uint[] Paint(IndexedSprite sprite, ThumbnailKey key)
    {
        var pixels = new uint[key.Width * key.Height];
        SpritePainter.Paint(sprite, key.Palette, key.Width, key.Height, pixels);
        return pixels;
    }
}
