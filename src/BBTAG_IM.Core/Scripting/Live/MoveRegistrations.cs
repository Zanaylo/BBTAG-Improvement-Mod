using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed class MoveRegistrations
{
    public const string Subroutine = "MatchInit";

    private const int StartLength = sizeof(int) + ScriptBinary.NameLength;
    private const int TempPriorityMultiplier = 15024;
    private const int TrainingCounterattack = 12059;
    private const int EndLength = sizeof(int);

    private static readonly HashSet<int> CommandsThatNameMoves = [TempPriorityMultiplier, TrainingCounterattack];

    private MoveRegistrations(IReadOnlyList<string> names, byte[] commands)
    {
        Names = names;
        Commands = commands;
    }

    public IReadOnlyList<string> Names { get; }

    public byte[] Commands { get; }

    public static MoveRegistrations Of(ScriptBinary script)
    {
        var names = new List<string>();
        var commands = new List<byte>();
        var start = (int?)null;

        foreach (var instruction in InMatchInit(script))
        {
            if (instruction.Id == ScriptCommands.StateRegister)
            {
                start = instruction.Offset;
                names.Add(instruction.TextAt(0));
                continue;
            }

            if (start is null && CommandsThatNameMoves.Contains(instruction.Id))
            {
                commands.AddRange(script.Bytes.AsSpan(instruction.Offset, instruction.Size));
                continue;
            }

            if (instruction.Id != ScriptCommands.EndStateRegister || start is not { } from)
                continue;

            commands.AddRange(script.Bytes.AsSpan(from, instruction.End - from));
            start = null;
        }

        return new MoveRegistrations(names, [.. commands]);
    }

    public bool SameAs(MoveRegistrations other) => Names.SequenceEqual(other.Names) && Commands.AsSpan().SequenceEqual(other.Commands);

    public byte[] AsSubroutine(string name)
    {
        var bytes = new byte[StartLength + Commands.Length + EndLength];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, ScriptCommands.StartSubroutine);
        Encoding.Latin1.GetBytes(name.AsSpan(0, Math.Min(name.Length, ScriptBinary.NameLength - 1)), bytes.AsSpan(sizeof(int)));
        Commands.CopyTo(bytes, StartLength);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(StartLength + Commands.Length), ScriptCommands.EndSubroutine);
        return bytes;
    }

    private static IEnumerable<ScriptInstruction> InMatchInit(ScriptBinary script) =>
        script.Instructions
            .SkipWhile(instruction => instruction.Id != ScriptCommands.StartSubroutine || instruction.TextAt(0) != Subroutine)
            .Skip(1)
            .TakeWhile(instruction => instruction.Id != ScriptCommands.EndSubroutine);
}
