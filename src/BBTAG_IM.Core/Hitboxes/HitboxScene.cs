using System.Numerics;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Hitboxes;

public sealed record HitboxShape(BoxKind Kind, Quad World);

public sealed record HitboxFrame(IReadOnlyList<HitboxShape> Boxes, IReadOnlyList<Vector2> Origins)
{
    public static HitboxFrame Empty { get; } = new([], []);
}

public static class HitboxScene
{
    private const int EntityListLength = GameView.EntityCount * sizeof(uint);

    public static bool IsVisible(GameView game, ImprovementSettings settings) =>
        settings.HitboxViewer && game.InTrainingMatch;

    public static HitboxFrame Collect(GameView game, ICharacterFates fates, float lift)
    {
        if (game.EntityList is not { } list || !game.Memory.CanRead(list, EntityListLength))
            return HitboxFrame.Empty;

        Span<byte> bytes = stackalloc byte[EntityListLength];
        game.Memory.Read(list, bytes);
        ReadOnlySpan<uint> entities = MemoryMarshal.Cast<byte, uint>(bytes);
        Dictionary<uint, BoxPlacement> placements = [];
        List<HitboxShape> boxes = [];
        List<Vector2> origins = [];

        for (var slot = 0; slot < entities.Length; slot++)
        {
            if (entities[slot] == 0 || Entity.Read(game.Memory, entities[slot]) is not { } entity)
                continue;

            if (slot < GameView.CharacterSlots)
                placements[entities[slot]] = new BoxPlacement(FateScale.Of(fates.Of(entity.CharacterIndex)), lift);
            else if (!entity.IsActive)
                continue;

            if (entity.Owner == 0 || !placements.TryGetValue(entity.Owner, out var placement) || entity.Boxes.Count == 0)
                continue;

            foreach (var box in entity.Boxes)
                boxes.Add(new HitboxShape(box.Kind, HitboxGeometry.Corners(entity, box, placement)));

            origins.Add(HitboxGeometry.Origin(entity, placement));
        }

        return new HitboxFrame(boxes, origins);
    }
}
