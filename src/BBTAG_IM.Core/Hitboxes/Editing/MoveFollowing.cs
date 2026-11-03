namespace BBTAG_IM.Core.Hitboxes.Editing;

public sealed record MovePosition(int Move, int Frame);

public sealed record ShownState(string State, string Sprite);

public static class MoveFollowing
{
    public static MovePosition? Find(IReadOnlyList<CharacterMove> moves, IEnumerable<ShownState> shown, MovePosition? current)
    {
        MovePosition? first = null;

        foreach (var showing in shown)
        {
            if (Locate(moves, showing, current) is not { } position)
                continue;

            if (position.Move == current?.Move)
                return position;

            first ??= position;
        }

        return first;
    }

    private static MovePosition? Locate(IReadOnlyList<CharacterMove> moves, ShownState showing, MovePosition? current)
    {
        for (var index = 0; index < moves.Count; index++)
        {
            if (moves[index].Name != showing.State)
                continue;

            var frame = moves[index].FrameShowing(showing.Sprite, index == current?.Move ? current.Frame : 0);
            return frame == CharacterMove.NotInTheMove ? null : new MovePosition(index, frame);
        }

        return null;
    }
}
