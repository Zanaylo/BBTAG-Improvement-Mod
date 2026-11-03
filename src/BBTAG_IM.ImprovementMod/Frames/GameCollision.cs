using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes;

namespace BBTAG_IM.ImprovementMod.Frames;

internal sealed unsafe class GameCollision(CollisionFunctions functions) : IGameCollision
{
    private const int PastTheLastBox = 0;
    private const int NoBox = 0;

    private readonly delegate* unmanaged[Thiscall]<nint, int, int, int*, int*, int*, int*, int*, int> _worldBox =
        (delegate* unmanaged[Thiscall]<nint, int, int, int*, int*, int*, int*, int*, int>)functions.WorldBox;

    private readonly delegate* unmanaged[Thiscall]<nint, int> _positionX = (delegate* unmanaged[Thiscall]<nint, int>)functions.PositionX;

    private readonly delegate* unmanaged[Thiscall]<nint, int> _positionY = (delegate* unmanaged[Thiscall]<nint, int>)functions.PositionY;

    public IReadOnlyList<GameRect> Boxes(uint entity, BoxKind kind)
    {
        List<GameRect> boxes = [];

        for (var index = 0; index < Entity.MaximumBoxes; index++)
        {
            if (!TryBox(entity, kind, index, out var box))
                break;

            if (box is { } found)
                boxes.Add(found);
        }

        return boxes;
    }

    public GameRect? Box(uint entity, BoxKind kind, int index) => TryBox(entity, kind, index, out var box) ? box : null;

    private bool TryBox(uint entity, BoxKind kind, int index, out GameRect? box)
    {
        int left, right, bottom, top, found;
        box = null;

        if (_worldBox((nint)entity, (int)kind, index, &left, &right, &bottom, &top, &found) == PastTheLastBox)
            return false;

        if (found != NoBox)
            box = new GameRect(left, right, bottom, top);

        return true;
    }

    public GamePoint Position(uint entity) => new(_positionX((nint)entity), _positionY((nint)entity));
}
