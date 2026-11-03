using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public sealed record MoveFrame(string Sprite, int Duration)
{
    private const string EmptySprite = "null";

    public bool ShowsASprite => !Sprite.Equals(EmptySprite, StringComparison.OrdinalIgnoreCase);
}

public sealed record CharacterMove(string Name, IReadOnlyList<MoveFrame> Frames, bool IsEffect = false)
{
    public const int NotInTheMove = -1;

    public int FrameShowing(string sprite, int from)
    {
        for (var step = 0; step < Frames.Count; step++)
        {
            var frame = (from + step) % Frames.Count;

            if (Frames[frame].Sprite == sprite)
                return frame;
        }

        return NotInTheMove;
    }
}

public static class CharacterMoves
{
    private const int StartState = 0;
    private const int EndState = 1;
    private const int Sprite = 2;
    private const int Upon = 15;
    private const int EndUpon = 16;
    private const int SpriteName = 0;
    private const int SpriteDuration = 1;

    public static IReadOnlyList<CharacterMove> Read(byte[] script, ScriptDatabase database, bool effects = false)
    {
        var states = ScriptBinary.ReadStates(script);
        var dataStart = ScriptBinary.DataStartFor(states.Count);
        var starts = states.Select(state => dataStart + state.Offset)
            .Where(start => start >= dataStart && start < script.Length)
            .Distinct()
            .Order()
            .ToList();
        List<CharacterMove> moves = [];

        for (var index = 0; index < starts.Count; index++)
        {
            var end = index + 1 < starts.Count ? starts[index + 1] : script.Length;

            if (InstructionsBetween(script, starts[index], end, database) is { } instructions)
                moves.AddRange(MovesIn(instructions, effects));
        }

        return moves;
    }

    private static IReadOnlyList<ScriptInstruction>? InstructionsBetween(byte[] script, int start, int end, ScriptDatabase database)
    {
        try
        {
            return ScriptBinary.ReadInstructions(script, start, end, database);
        }
        catch (InvalidScriptException)
        {
            return null;
        }
    }

    private static List<CharacterMove> MovesIn(IReadOnlyList<ScriptInstruction> instructions, bool effects)
    {
        List<CharacterMove> moves = [];
        string? state = null;
        List<MoveFrame> frames = [];
        var uponDepth = 0;

        foreach (var instruction in instructions)
        {
            switch (instruction.Id)
            {
                case StartState:
                    state = instruction.TextAt(SpriteName);
                    frames = [];
                    uponDepth = 0;
                    break;
                case Upon:
                    uponDepth++;
                    break;
                case EndUpon:
                    uponDepth = Math.Max(uponDepth - 1, 0);
                    break;
                case Sprite when state is not null && uponDepth == 0:
                    frames.Add(new MoveFrame(instruction.TextAt(SpriteName), instruction.Values[SpriteDuration].Number));
                    break;
                case EndState when state is not null:
                    if (frames.Count > 0)
                        moves.Add(new CharacterMove(state, frames, effects));

                    state = null;
                    break;
            }
        }

        return moves;
    }
}
