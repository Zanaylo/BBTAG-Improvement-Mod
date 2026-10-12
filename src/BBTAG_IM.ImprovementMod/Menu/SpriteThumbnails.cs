using System.Collections.Concurrent;
using System.Numerics;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Sprites;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class SpriteThumbnails
{
    private const int CachedTextures = 192;
    private const int UploadsPerFrame = 4;
    private const float Half = 0.5f;

    private readonly CharacterSprites _sprites;
    private readonly MenuTextures _textures;
    private readonly ThumbnailPainting _painting = new();
    private readonly ConcurrentDictionary<string, IndexedSprite?> _spriteByFolder = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _spritesRequested = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ThumbnailKey, Thumbnail> _thumbnails = [];

    private int _frame = -1;
    private int _uploads;

    public SpriteThumbnails(CharacterSprites sprites, MenuTextures textures)
    {
        _sprites = sprites;
        _textures = textures;
        _textures.Released += _thumbnails.Clear;
    }

    public bool Draw(ImDrawListPtr draw, string folder, ImplPalette palette, Vector2 topLeft, Vector2 box)
    {
        StartFrame();

        if (Sprite(folder) is not { } sprite)
            return false;

        var (width, height) = SpritePainter.Fit(sprite, (int)box.X, (int)box.Y);

        if (Texture(new ThumbnailKey(palette, width, height), sprite) is not { } texture)
            return false;

        var corner = topLeft + new Vector2((box.X - width) * Half, box.Y - height);
        NativeImGui.AddImage(draw, texture, corner, corner + new Vector2(width, height));
        return true;
    }

    private void StartFrame()
    {
        var frame = ImGui.GetFrameCount();

        if (frame == _frame)
            return;

        _frame = frame;
        _uploads = 0;
    }

    private IndexedSprite? Sprite(string folder)
    {
        if (_spriteByFolder.TryGetValue(folder, out var sprite))
            return sprite;

        if (_spritesRequested.Add(folder))
            Task.Run(() => _spriteByFolder[folder] = _sprites.Load(folder));

        return null;
    }

    private nint? Texture(ThumbnailKey key, IndexedSprite sprite)
    {
        if (_thumbnails.TryGetValue(key, out var thumbnail))
        {
            thumbnail.LastFrame = _frame;
            return thumbnail.Texture;
        }

        if (_uploads >= UploadsPerFrame || !_painting.TryTake(key, out var pixels))
        {
            _painting.Want(key, sprite, _frame);
            return null;
        }

        _uploads++;
        return Upload(key, pixels);
    }

    private nint? Upload(ThumbnailKey key, uint[] pixels)
    {
        var texture = _textures.Create(key.Width, key.Height, pixels);

        if (texture == 0)
            return null;

        MakeRoom();
        _thumbnails[key] = new Thumbnail(texture, _frame);
        return texture;
    }

    private void MakeRoom()
    {
        if (_thumbnails.Count < CachedTextures)
            return;

        var oldest = _thumbnails.MinBy(entry => entry.Value.LastFrame);

        if (oldest.Value.LastFrame == _frame)
            return;

        _textures.Release(oldest.Value.Texture);
        _thumbnails.Remove(oldest.Key);
    }

    private sealed class Thumbnail(nint texture, int lastFrame)
    {
        public nint Texture => texture;

        public int LastFrame { get; set; } = lastFrame;
    }
}
