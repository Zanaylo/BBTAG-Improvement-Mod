using System.Globalization;
using System.Text;

namespace BBTAG_IM.Core.Scripting;

public static class CommandSizesText
{
    private const char Separator = '\t';
    private const char LineEnd = '\n';

    public static IReadOnlyDictionary<int, int> Read(string text)
    {
        var sizes = new Dictionary<int, int>();

        foreach (var line in text.Split(LineEnd))
        {
            if (line.TrimEnd().Split(Separator) is [var id, var size])
                sizes[int.Parse(id, CultureInfo.InvariantCulture)] = int.Parse(size, CultureInfo.InvariantCulture);
        }

        return sizes;
    }

    public static string Write(IReadOnlyDictionary<int, int> sizes)
    {
        var text = new StringBuilder();

        foreach (var (id, size) in sizes.OrderBy(pair => pair.Key))
            text.Append(CultureInfo.InvariantCulture, $"{id}{Separator}{size}{LineEnd}");

        return text.ToString();
    }
}
