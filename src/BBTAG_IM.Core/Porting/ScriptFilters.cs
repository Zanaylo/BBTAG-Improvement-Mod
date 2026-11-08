using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Porting;

public static partial class ScriptFilters
{
    private const char LineBreak = '\n';
    private const string RegisterDecorator = "@StateRegister";
    private const string NoOperation = "pass";

    public static string DropSpecialMoves(string script)
    {
        var lines = script.Split(LineBreak);
        List<string> output = [];

        for (var index = 0; index < lines.Length; index++)
        {
            if (!IsSpecialRegistration(lines, index))
            {
                output.Add(lines[index]);
                continue;
            }

            index = LastLineOfBlock(lines, index + 1);
        }

        return string.Join(LineBreak, output);
    }

    public static string Silence(string script) => TemplateCommand().Replace(script, match => match.Groups[1].Value + NoOperation);

    [GeneratedRegex(@"def \w+\(INPUT_(SPECIALMOVE|DISTORTION|ASTRAL)\):\s*$")]
    private static partial Regex SpecialRegistration();

    [GeneratedRegex(@"^([ \t]*)(?:Voiceline|RandomCommonVoiceline|PrivateSE|GFX_0|GFX_1)\(.*\)[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex TemplateCommand();

    private static bool IsSpecialRegistration(string[] lines, int index) =>
        lines[index].Trim() == RegisterDecorator && index + 1 < lines.Length && SpecialRegistration().IsMatch(lines[index + 1]);

    private static int LastLineOfBlock(string[] lines, int definition)
    {
        var indent = Indent(lines[definition]);
        var last = definition;

        for (var index = definition + 1; index < lines.Length; index++)
        {
            if (lines[index].Trim().Length == 0)
                continue;

            if (Indent(lines[index]) <= indent)
                break;

            last = index;
        }

        return last;
    }

    private static int Indent(string line) => line.Length - line.TrimStart().Length;
}
