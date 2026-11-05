namespace BBTAG_IM.Core.Scripting.Importing;

public sealed record NameImportResult(string Json, int Named, int Added, IReadOnlyList<string> Skipped);
