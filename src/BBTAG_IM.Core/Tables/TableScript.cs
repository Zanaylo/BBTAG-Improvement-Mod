namespace BBTAG_IM.Core.Tables;

public sealed record TableScript(int Id, string Description, bool CanToggle, IReadOnlyList<TableScript> Children);
