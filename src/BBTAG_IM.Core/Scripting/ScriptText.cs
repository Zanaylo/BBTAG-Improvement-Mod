using System.Text;

namespace BBTAG_IM.Core.Scripting;

public static class ScriptText
{
    public static string Decode(ReadOnlySpan<byte> field)
    {
        var length = field.Length;

        while (length > 0 && field[length - 1] == 0)
            length--;

        return Encoding.Latin1.GetString(field[..length]);
    }

    public static string Name(ReadOnlySpan<byte> field)
    {
        var terminator = field.IndexOf((byte)0);
        return Encoding.Latin1.GetString(terminator < 0 ? field : field[..terminator]);
    }

    public static byte[] Encode(string text, int length)
    {
        if (text.Any(letter => letter > 0xFF))
            throw new ArgumentException($"\"{text}\" has letters that do not fit in one byte.", nameof(text));

        if (text.Length > length)
            throw new ArgumentException($"\"{text}\" is longer than {length} letters.", nameof(text));

        var field = new byte[length];
        Encoding.Latin1.GetBytes(text, field);
        return field;
    }
}
