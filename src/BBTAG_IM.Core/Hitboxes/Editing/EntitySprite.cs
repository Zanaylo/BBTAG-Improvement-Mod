using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public sealed record EntitySprite(uint Entity, uint Archive, string Name, uint JonbinAddress)
{
    private const uint JonbinField = 0x40;
    private const uint ArchiveField = 0x154;
    private const uint NameField = 0x1420;
    private const uint StateField = 0x25DC;
    private const int NameLength = 32;

    public string FileName => Name + Jonbin.Extension;

    public static string? StateOf(IGameMemory memory, uint entity) =>
        memory.CanRead(entity + StateField, NameLength) ? memory.ReadText(entity + StateField, NameLength) : null;

    public static EntitySprite? Read(IGameMemory memory, uint entity)
    {
        if (!memory.CanRead(entity, (int)NameField + NameLength))
            return null;

        var archive = memory.ReadUInt32(entity + ArchiveField);
        var name = memory.ReadText(entity + NameField, NameLength);

        if (archive == 0 || name.Length == 0)
            return null;

        return new EntitySprite(entity, archive, name, memory.ReadUInt32(entity + JonbinField));
    }
}
