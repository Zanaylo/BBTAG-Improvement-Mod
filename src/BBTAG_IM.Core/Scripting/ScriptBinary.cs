using System.Buffers.Binary;

namespace BBTAG_IM.Core.Scripting;

public sealed record ScriptState(string Name, int Offset);

public sealed class ScriptBinary
{
    public const int NameLength = 32;
    public const int StateEntryLength = NameLength + sizeof(int);

    private ScriptBinary(byte[] bytes, IReadOnlyList<ScriptState> states, IReadOnlyList<ScriptInstruction> instructions)
    {
        Bytes = bytes;
        States = states;
        Instructions = instructions;
    }

    public byte[] Bytes { get; }

    public IReadOnlyList<ScriptState> States { get; }

    public IReadOnlyList<ScriptInstruction> Instructions { get; }

    public int DataStart => DataStartFor(States.Count);

    public static int DataStartFor(int stateCount) => sizeof(int) + stateCount * StateEntryLength;

    public static ScriptBinary Read(byte[] bytes, ScriptDatabase database)
    {
        var states = ReadStates(bytes);
        var instructions = new List<ScriptInstruction>();
        var offset = DataStartFor(states.Count);

        while (offset < bytes.Length)
        {
            var instruction = ReadInstruction(bytes, offset, database);
            instructions.Add(instruction);
            offset = instruction.End;
        }

        return new ScriptBinary(bytes, states, instructions);
    }

    public static IReadOnlyList<ScriptState> ReadStates(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < sizeof(int))
            throw new InvalidScriptException("The file is too short to be a script.");

        var count = BinaryPrimitives.ReadInt32LittleEndian(bytes);

        if (count < 0 || DataStartFor(count) > bytes.Length)
            throw new InvalidScriptException($"The file says it has {count} states, which does not fit in it.");

        var states = new ScriptState[count];

        for (var index = 0; index < count; index++)
        {
            var entry = bytes.Slice(sizeof(int) + index * StateEntryLength, StateEntryLength);
            states[index] = new ScriptState(ScriptText.Name(entry[..NameLength]), BinaryPrimitives.ReadInt32LittleEndian(entry[NameLength..]));
        }

        return states;
    }

    private static ScriptInstruction ReadInstruction(byte[] bytes, int offset, ScriptDatabase database)
    {
        if (offset + sizeof(int) > bytes.Length)
            throw new InvalidScriptException($"The file ends in the middle of a command at 0x{offset:X}.");

        var id = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));
        var command = database.Find(id) ?? throw new InvalidScriptException($"Command {id} at 0x{offset:X} is not in the script database.");

        if (offset + command.Size > bytes.Length)
            throw new InvalidScriptException($"{command.Name} at 0x{offset:X} runs past the end of the file.");

        var values = new ScriptValue[command.Arguments.Count];
        var position = offset + sizeof(int);

        for (var index = 0; index < values.Length; index++)
        {
            var argument = command.Arguments[index];
            values[index] = argument.Kind == ArgumentKind.Text
                ? ScriptValue.Of(bytes.AsSpan(position, argument.Length).ToArray())
                : ScriptValue.Of(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(position)));
            position += argument.Length;
        }

        return new ScriptInstruction(offset, command, values);
    }
}
