using System.Buffers.Binary;

namespace BBTAG_IM.Core.Scripting.Compiling;

public sealed class ScriptBuffer
{
    private readonly List<byte> bytes = [];
    private readonly List<ScriptLine> lines = [];

    public int Length => bytes.Count;

    public void WriteInt32(int value)
    {
        Span<byte> field = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(field, value);
        bytes.AddRange(field);
    }

    public void WriteBytes(ReadOnlySpan<byte> field) => bytes.AddRange(field);

    public void PatchInt32(int offset, int value)
    {
        Span<byte> field = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(field, value);

        for (var index = 0; index < field.Length; index++)
            bytes[offset + index] = field[index];
    }

    public void MarkLine(int line)
    {
        if (lines.Count > 0 && lines[^1].Offset == Length)
            lines[^1] = new ScriptLine(Length, line);
        else
            lines.Add(new ScriptLine(Length, line));
    }

    public CompiledScript ToCompiledScript() => new([.. bytes], new ScriptLineMap([.. lines]));
}
