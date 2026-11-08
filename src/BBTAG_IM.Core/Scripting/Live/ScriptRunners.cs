using System.Buffers.Binary;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed record StaleRunner(uint Entity, string State);

public sealed class ScriptRunners(GameView game)
{
    public const uint CurrentInstructionOffset = 0x1408;
    public const uint StateStartOffset = 0x140C;
    public const uint StateNameOffset = 0x25DC;

    private const int EntityListLength = GameView.EntityCount * sizeof(uint);
    private const int ScriptFieldsLength = (int)(StateStartOffset - ScriptRecord.ObjectRecordOffset) + sizeof(uint);

    public IReadOnlyList<uint> InstructionsOf(uint record) =>
        EntitiesOf(record).Select(entity => game.Memory.ReadUInt32(entity + CurrentInstructionOffset)).ToList();

    public IReadOnlyList<StaleRunner> StaleOf(uint record, uint file, int length) =>
        EntitiesOf(record)
            .Where(entity => game.Memory.ReadUInt32(entity + StateStartOffset) - file >= (uint)length)
            .Select(entity => new StaleRunner(entity, game.Memory.ReadText(entity + StateNameOffset, ScriptBinary.NameLength)))
            .ToList();

    private List<uint> EntitiesOf(uint record)
    {
        var memory = game.Memory;

        if (game.EntityList is not { } list || !memory.CanRead(list, EntityListLength))
            return [];

        var entities = memory.ReadBytes(list, EntityListLength);
        var found = new List<uint>();

        for (var index = 0; index < GameView.EntityCount; index++)
        {
            var entity = BinaryPrimitives.ReadUInt32LittleEndian(entities.AsSpan(index * sizeof(uint)));
            var fields = entity + ScriptRecord.ObjectRecordOffset;

            if (entity == 0 || !memory.CanRead(fields, ScriptFieldsLength) || memory.ReadUInt32(fields) != record)
                continue;

            found.Add(entity);
        }

        return found;
    }
}
