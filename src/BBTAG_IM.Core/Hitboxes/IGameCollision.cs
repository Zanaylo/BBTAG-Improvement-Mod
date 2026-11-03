namespace BBTAG_IM.Core.Hitboxes;

public readonly record struct GamePoint(int X, int Y);

public readonly record struct GameRect(int Left, int Right, int Bottom, int Top);

public interface IGameCollision
{
    IReadOnlyList<GameRect> Boxes(uint entity, BoxKind kind);

    GameRect? Box(uint entity, BoxKind kind, int index);

    GamePoint Position(uint entity);
}
