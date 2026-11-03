using System.Runtime.InteropServices;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public static class ShownSprites
{
    private const int EntityListLength = GameView.EntityCount * sizeof(uint);
    private const uint StatusField = 0x10;
    private const uint OwnerField = 0x1F4;

    public static IReadOnlyList<EntitySprite> Of(GameView game, uint character)
    {
        List<EntitySprite> shown = [];

        if (game.EntityList is not { } list || !game.Memory.CanRead(list, EntityListLength))
            return shown;

        var bytes = game.Memory.ReadBytes(list, EntityListLength);

        foreach (var entity in MemoryMarshal.Cast<byte, uint>(bytes))
        {
            if (entity != 0 && Belongs(game.Memory, entity, character) && EntitySprite.Read(game.Memory, entity) is { } sprite)
                shown.Add(sprite);
        }

        return shown;
    }

    private static bool Belongs(IGameMemory memory, uint entity, uint character)
    {
        if (entity == character)
            return true;

        return memory.CanRead(entity, (int)OwnerField + sizeof(uint))
            && memory.ReadUInt32(entity + OwnerField) == character
            && memory.ReadInt32(entity + StatusField) == Entity.ActiveStatus;
    }
}
