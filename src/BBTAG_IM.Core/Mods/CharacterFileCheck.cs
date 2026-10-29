namespace BBTAG_IM.Core.Mods;

public sealed class CharacterFileCheck(IGameFiles gameFiles)
{
    public IReadOnlyList<CharacterFileReport> Run(IEnumerable<string> codes) =>
        codes.Select(Check)
            .Where(report => report.Missing.Count > 0)
            .ToArray();

    private CharacterFileReport Check(string code)
    {
        var required = CharacterFileRules.For(code);
        var missing = required
            .Where(file => !file.Choices.Any(gameFiles.Has))
            .Select(file => file.Name)
            .ToArray();
        return new CharacterFileReport(code, missing, required.Count);
    }
}
