using System.Text;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed record ScriptMapNode(uint Address, string Name, int Offset);

public sealed class ScriptMap(IGameMemory memory, uint address)
{
    public const int NodeLength = 0x34;
    public const uint NodeValueOffset = 0x30;

    private const uint BucketsOffset = 0x10;
    private const uint BucketCountOffset = 0x14;
    private const uint BucketLength = 0x10;
    private const uint BucketHeadOffset = 4;
    private const uint BucketTailOffset = 8;
    private const uint BucketCountInBucketOffset = 0xC;
    private const uint NodeNextOffset = 4;
    private const uint NodePreviousOffset = 8;
    private const uint NodeKeyTypeOffset = 0xC;
    private const uint NodeNameOffset = 0x10;
    private const int MostNodes = 0x10000;

    public IReadOnlyList<ScriptMapNode> Nodes()
    {
        var nodes = new List<ScriptMapNode>();
        var buckets = memory.ReadUInt32(address + BucketsOffset);
        var count = memory.ReadUInt32(address + BucketCountOffset);

        for (var bucket = 0u; bucket < count; bucket++)
        {
            var node = memory.ReadUInt32(buckets + bucket * BucketLength + BucketHeadOffset);

            while (node != 0 && nodes.Count < MostNodes)
            {
                nodes.Add(new ScriptMapNode(node, memory.ReadText(node + NodeNameOffset, ScriptBinary.NameLength), memory.ReadInt32(node + NodeValueOffset)));
                node = memory.ReadUInt32(node + NodeNextOffset);
            }
        }

        return nodes;
    }

    public uint Add(string name, int offset, uint template, IGameHeap heap)
    {
        var key = name[..Math.Min(name.Length, ScriptBinary.NameLength - 1)];
        var node = heap.Allocate(NodeLength);
        var bytes = new byte[NodeLength];
        var bucket = BucketOf(key);
        var tail = memory.ReadUInt32(bucket + BucketTailOffset);

        Encoding.Latin1.GetBytes(key, bytes.AsSpan((int)NodeNameOffset));
        memory.Write(node, bytes);
        memory.WriteInt32(node, memory.ReadInt32(template));
        memory.WriteInt32(node + NodeKeyTypeOffset, memory.ReadInt32(template + NodeKeyTypeOffset));
        memory.WriteInt32(node + NodeValueOffset, offset);
        memory.WriteInt32(node + NodePreviousOffset, (int)tail);
        memory.WriteInt32(tail == 0 ? bucket + BucketHeadOffset : tail + NodeNextOffset, (int)node);
        memory.WriteInt32(bucket + BucketTailOffset, (int)node);
        memory.WriteInt32(bucket + BucketCountInBucketOffset, memory.ReadInt32(bucket + BucketCountInBucketOffset) + 1);
        return node;
    }

    public void Remove(ScriptMapNode node)
    {
        var bucket = BucketOf(node.Name);
        var previous = 0u;
        var current = memory.ReadUInt32(bucket + BucketHeadOffset);

        for (var visited = 0; current != 0 && current != node.Address && visited < MostNodes; visited++)
        {
            previous = current;
            current = memory.ReadUInt32(current + NodeNextOffset);
        }

        if (current != node.Address)
            return;

        var next = memory.ReadUInt32(node.Address + NodeNextOffset);
        memory.WriteInt32(previous == 0 ? bucket + BucketHeadOffset : previous + NodeNextOffset, (int)next);
        memory.WriteInt32(next == 0 ? bucket + BucketTailOffset : next + NodePreviousOffset, (int)previous);
        memory.WriteInt32(bucket + BucketCountInBucketOffset, memory.ReadInt32(bucket + BucketCountInBucketOffset) - 1);
    }

    private uint BucketOf(string name) =>
        memory.ReadUInt32(address + BucketsOffset) + ScriptMapHash.Of(name) % memory.ReadUInt32(address + BucketCountOffset) * BucketLength;
}
