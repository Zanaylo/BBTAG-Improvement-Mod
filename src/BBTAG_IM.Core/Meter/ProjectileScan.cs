using System.Runtime.InteropServices;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Meter;

public static class ProjectileScan
{
    public const int ListLength = GameView.EntityCount * sizeof(uint);
    public const int EntityLength = OwnerField + sizeof(uint);

    private const uint StatusField = 0x10;
    private const uint HitboxCountField = 0x98;
    private const int OwnerField = 0x1F4;
    private const int ActiveStatus = 1;

    public static IReadOnlyList<bool> Owners(IGameMemory memory, uint entityList, IReadOnlyList<uint?> characters)
    {
        var owners = new bool[characters.Count];

        if (!memory.CanRead(entityList, ListLength))
            return owners;

        var bytes = new byte[ListLength];
        memory.Read(entityList, bytes);

        foreach (var entity in MemoryMarshal.Cast<byte, uint>(bytes))
            MarkOwner(memory, entity, characters, owners);

        return owners;
    }

    private static void MarkOwner(IGameMemory memory, uint entity, IReadOnlyList<uint?> characters, bool[] owners)
    {
        if (entity == 0 || IsCharacter(entity, characters) || !memory.CanRead(entity, EntityLength))
            return;

        if (memory.ReadInt32(entity + StatusField) != ActiveStatus || memory.ReadInt32(entity + HitboxCountField) <= 0)
            return;

        var owner = memory.ReadUInt32(entity + OwnerField);

        for (var slot = 0; slot < characters.Count; slot++)
        {
            if (characters[slot] == owner)
                owners[slot] = true;
        }
    }

    private static bool IsCharacter(uint entity, IReadOnlyList<uint?> characters) => characters.Any(character => character == entity);
}
