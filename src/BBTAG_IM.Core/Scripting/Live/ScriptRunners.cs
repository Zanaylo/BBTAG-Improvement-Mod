using System.Buffers.Binary;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed class ScriptRunners(GameView game)
{
    public const uint CurrentInstructionOffset = 0x1408;

    private const int EntityListLength = GameView.EntityCount * sizeof(uint);
    private const int ScriptFieldsLength = (int)(CurrentInstructionOffset - ScriptRecord.ObjectRecordOffset) + sizeof(uint);

    public IReadOnlyList<uint> InstructionsOf(uint record)
    {
        var memory = game.Memory;

        if (game.EntityList is not { } list || !memory.CanRead(list, EntityListLength))
            return [];

        var entities = memory.ReadBytes(list, EntityListLength);
        var instructions = new List<uint>();

        for (var index = 0; index < GameView.EntityCount; index++)
        {
            var entity = BinaryPrimitives.ReadUInt32LittleEndian(entities.AsSpan(index * sizeof(uint)));
            var fields = entity + ScriptRecord.ObjectRecordOffset;

            if (entity == 0 || !memory.CanRead(fields, ScriptFieldsLength) || memory.ReadUInt32(fields) != record)
                continue;

            instructions.Add(memory.ReadUInt32(entity + CurrentInstructionOffset));
        }

        return instructions;
    }
}
