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

    private readonly CharacterImages _images;
    private readonly MenuTextures _textures;
    private readonly Func<int, PaletteColors?> _wornColors;
    private readonly Dictionary<string, nint> _cache = [];
    private readonly Dictionary<SpritePiece, PlacedChunk?> _placements = [];

    public SpritePreview(CharacterImages images, MenuTextures textures, Func<int, PaletteColors?> wornColors)
    {
        _images = images;
        _textures = textures;
        _wornColors = wornColors;
        _textures.Released += _cache.Clear;
    }

    public IEnumerable<JonbinBox> Outline(DevCharacter character, Jonbin jonbin)
    {
        foreach (var piece in jonbin.Pieces)
        {
            if (Image(character, piece.Image) is { } image && Placed(piece, image) is { } placed)
                yield return new JonbinBox(0, placed.X, placed.Y, placed.Width, placed.Height);
        }
    }

    public void Draw(ImDrawListPtr draw, DevCharacter character, Jonbin jonbin, BoxCanvas canvas)
    {
        if (_cache.Count >= CachedTextures)
            Forget();

        foreach (var piece in jonbin.Pieces)
        {
            if (Image(character, piece.Image) is not { } image || Placed(piece, image) is not { } placed)
                continue;

            if (Texture(character.Slot, character.Code, piece.Image, image) is not { } texture)
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

    private PlacedChunk? Placed(SpritePiece piece, CharacterImage image)
    {
        if (_placements.TryGetValue(piece, out var known))
            return known;

        var placed = ChunkPlacement.Place(piece.Chunk, image.Sprite, image.PixelsPerUnit);
        _placements[piece] = placed;
        return placed;
    }

    private CharacterImage? Image(DevCharacter character, string name) => _images.Load(character.Code, character.Folder, name);

    private nint? Texture(int slot, string code, string name, CharacterImage image)
    {
        var key = $"{slot}/{code}/{name}";

        if (_cache.TryGetValue(key, out var known))
            return known;

        var colors = !image.IsEffect && _wornColors(slot) is { } worn ? SpritePainter.CharacterColors(worn) : image.Sprite.Colors;
        var sprite = image.Sprite.Image;
        var texture = _textures.Create(sprite.Width, sprite.Height, SpritePainter.Colored(sprite, colors));

        if (texture == 0)
            return null;

        _cache[key] = texture;
        return texture;
    }
}
