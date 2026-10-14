using System.Buffers.Binary;
using System.Text;
using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed class ScriptBytes
{
    private readonly List<(string Name, int Offset)> _states = [];
    private readonly List<byte> _code = [];

    public ScriptBytes State(string name)
    {
        _states.Add((name, _code.Count));
        return Command(0, name);
    }

    public ScriptBytes Subroutine(string name) => Command(8, name);

    public ScriptBytes Command(int id, params object[] values)
    {
        Int32(id);

        foreach (var value in values)
        {
            if (value is string text)
                Text(text);
            else
                Int32((int)value);
        }

        return this;
    }

    public byte[] Build()
    {
        var bytes = new List<byte>();
        AddInt32(bytes, _states.Count);

        foreach (var (name, offset) in _states)
        {
            bytes.AddRange(Field(name));
            AddInt32(bytes, offset);
        }

        bytes.AddRange(_code);
        return [.. bytes];
    }

    private void Int32(int value) => AddInt32(_code, value);

    private void Text(string text) => _code.AddRange(Field(text));

    private static byte[] Field(string text)
    {
        var field = new byte[ScriptBinary.NameLength];
        Encoding.Latin1.GetBytes(text, field);
        return field;
    }

    private static void AddInt32(List<byte> bytes, int value)
    {
        Span<byte> field = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(field, value);
        bytes.AddRange(field);
    }
}
