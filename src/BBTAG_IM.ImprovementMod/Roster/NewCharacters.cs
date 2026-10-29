using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Roster;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Roster;

internal static class NewCharacters
{
    public static IReadOnlyList<RosterAddition> Install(CodeImage code, IGameMemory memory, ManifestLoad manifests, ILog log)
    {
        foreach (var problem in manifests.Problems)
            log.Error(problem);

        if (manifests.Manifests.Count == 0)
            return [];

        var plan = RosterPlan.Create(manifests.Manifests, RosterCodeTable.Read(code, memory));

        foreach (var problem in plan.Problems)
            log.Error(problem);

        if (plan.Additions.Count == 0)
            return [];

        AddToRoster(code, plan.Additions);
        AddToGrid(code, plan.Additions);
        UseTemplates(code, plan.Additions);

        foreach (var addition in plan.Additions)
            log.Info($"{addition.Manifest.Code} joins the roster at index {addition.Index}, plays like {addition.Manifest.Template} and sits in the {addition.Manifest.Series} row.");

        return plan.Additions;
    }

    private static void AddToRoster(CodeImage code, IReadOnlyList<RosterAddition> additions)
    {
        var site = RosterFillCode.Locate(code);
        var cave = CodeCave.Reserve(RosterFillCode.BlockLength, "new roster characters");
        cave.Put(RosterFillCode.NamesOffset, RosterFillCode.Names(additions));
        cave.Put(RosterFillCode.CodeOffset, RosterFillCode.Code(site, cave.Address, additions));
        CodeCave.Redirect(site, RosterFillCode.Jump(site, cave.Address));
    }

    private static void AddToGrid(CodeImage code, IReadOnlyList<RosterAddition> additions)
    {
        var site = CharacterGridCode.Locate(code);
        var cave = CodeCave.Reserve(CharacterGridCode.BlockLength, "character select grid");
        cave.Put(CharacterGridCode.CodeOffset, CharacterGridCode.Code(site, cave.Address, additions));
        CodeCave.Redirect(site, CharacterGridCode.Jump(site, cave.Address));
    }

    private static void UseTemplates(CodeImage code, IReadOnlyList<RosterAddition> additions)
    {
        var site = TemplateIndexCode.Locate(code);
        var cave = CodeCave.Reserve(TemplateIndexCode.BlockLength, "character templates");
        cave.Put(TemplateIndexCode.MapOffset, TemplateIndexCode.Map(additions));
        cave.Put(TemplateIndexCode.CodeOffset, TemplateIndexCode.Code(site, cave.Address));
        CodeCave.Redirect(site, TemplateIndexCode.Jump(site, cave.Address));
    }
}
