using System.Globalization;
using System.Text;

namespace BBTAG_IM.Core.Scripting;

public static class ScriptLiterals
{
    private const char Quote = '\'';
    private const char FirstPrintable = ' ';
    private const char LastPrintable = '~';

    public static string Text(string text)
    {
        var literal = new StringBuilder(text.Length + 2).Append(Quote);

        foreach (var letter in text)
            literal.Append(Escaped(letter));

        return literal.Append(Quote).ToString();
    }

    public static string Number(int value, bool hex, bool unsigned)
    {
        if (unsigned)
            return hex ? "0x" + ((uint)value).ToString("x", CultureInfo.InvariantCulture) : ((uint)value).ToString(CultureInfo.InvariantCulture);

        if (!hex)
            return value.ToString(CultureInfo.InvariantCulture);

        var magnitude = Math.Abs((long)value).ToString("x", CultureInfo.InvariantCulture);
        return value < 0 ? "-0x" + magnitude : "0x" + magnitude;
    }

    private static string Escaped(char letter) => letter switch
    {
        '\\' => @"\\",
        Quote => @"\'",
        '\n' => @"\n",
        '\r' => @"\r",
        '\t' => @"\t",
        >= FirstPrintable and <= LastPrintable => letter.ToString(),
        _ => @"\x" + ((int)letter).ToString("x2", CultureInfo.InvariantCulture),
    };
}
