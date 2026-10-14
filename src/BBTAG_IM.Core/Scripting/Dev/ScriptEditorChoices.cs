namespace BBTAG_IM.Core.Scripting.Dev;

public sealed record ScriptEditorChoice(string Name, string Program);

public static class ScriptEditorChoices
{
    public static IReadOnlyList<ScriptEditorChoice> Merge(IEnumerable<ScriptEditorChoice> found) => found
        .Where(choice => choice.Program.Length > 0)
        .DistinctBy(choice => choice.Program, StringComparer.OrdinalIgnoreCase)
        .Select(choice => choice.Name.Length > 0 ? choice : choice with { Name = FileNameOf(choice.Program) })
        .ToList();

    public static string NameOf(string program, IReadOnlyList<ScriptEditorChoice> choices) =>
        choices.FirstOrDefault(choice => choice.Program.Equals(program, StringComparison.OrdinalIgnoreCase))?.Name ?? FileNameOf(program);

    private static string FileNameOf(string program) => Path.GetFileNameWithoutExtension(program);
}
