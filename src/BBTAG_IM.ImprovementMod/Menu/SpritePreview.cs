using System.Numerics;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.ImprovementMod.Dev;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class SpritePreview
{
    private const int CachedTextures = 48;
    private const string PlaceholderImage = "999_99";

    private readonly CharacterImages _images;
    private readonly MenuTextures _textures;
    private readonly Func<int, PaletteColors?> _wornColors;
    private readonly Dictionary<string, nint> _cache = [];
    private readonly Dictionary<(string Image, SpriteChunk Chunk), PlacedChunk?> _placements = [];

    public SpritePreview(CharacterImages images, MenuTextures textures, Func<int, PaletteColors?> wornColors)
    {
        _images = images;
        _textures = textures;
        _wornColors = wornColors;
        _textures.Released += _cache.Clear;
    }

    public IEnumerable<JonbinBox> Outline(DevCharacter character, Jonbin jonbin)
    {
        if (Picture(character, jonbin) is not { } picture)
            yield break;

        foreach (var chunk in jonbin.Chunks)
        {
            if (Placed(picture.Name, picture.Image, chunk) is { } placed)
                yield return new JonbinBox(0, placed.X, placed.Y, placed.Width, placed.Height);
        }
    }

    public void Draw(ImDrawListPtr draw, DevCharacter character, Jonbin jonbin, BoxCanvas canvas)
    {
        if (Picture(character, jonbin) is not { } picture || Texture(character.Slot, character.Code, picture.Name, picture.Image) is not { } texture)
            return;

        foreach (var chunk in jonbin.Chunks.OrderBy(chunk => chunk.Layer))
        {
            if (Placed(picture.Name, picture.Image, chunk) is not { } placed)
                continue;

            var area = canvas.ToCanvas(new JonbinBox(0, placed.X, placed.Y, placed.Width, placed.Height));
            NativeImGui.AddImage(
                draw,
                texture,
                new Vector2(area.Left, area.Top),
                new Vector2(area.Right, area.Bottom),
                new Vector2(placed.UvMax.X, placed.UvMin.Y),
                new Vector2(placed.UvMin.X, placed.UvMax.Y));
        }
    }

    public void Forget()
    {
        foreach (var texture in _cache.Values)
            _textures.Release(texture);

        _cache.Clear();
        _placements.Clear();
    }

    private PlacedChunk? Placed(string name, CharacterImage image, SpriteChunk chunk)
    {
        if (_placements.TryGetValue((name, chunk), out var known))
            return known;

        var placed = ChunkPlacement.Place(chunk, image.Sprite, image.PixelsPerUnit);
        _placements[(name, chunk)] = placed;
        return placed;
    }

    private (string Name, CharacterImage Image)? Picture(DevCharacter character, Jonbin jonbin)
    {
        var name = jonbin.Images.FirstOrDefault(image => !Path.GetFileNameWithoutExtension(image).EndsWith(PlaceholderImage, StringComparison.Ordinal));

        if (name is null || _images.Load(character.Code, character.Folder, name) is not { } image)
            return null;

        return (name, image);
    }

    private nint? Texture(int slot, string code, string name, CharacterImage image)
    {
        var key = $"{slot}/{code}/{name}";

        if (_cache.TryGetValue(key, out var known))
            return known;

        if (_cache.Count >= CachedTextures)
            Forget();

        var colors = !image.IsEffect && _wornColors(slot) is { } worn ? SpritePainter.CharacterColors(worn) : image.Sprite.Colors;
        var sprite = image.Sprite.Image;
        var texture = _textures.Create(sprite.Width, sprite.Height, SpritePainter.Colored(sprite, colors));

        if (texture == 0)
            return null;

        _cache[key] = texture;
        return texture;
    }
}
