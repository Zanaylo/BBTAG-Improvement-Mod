using System.Buffers.Binary;

namespace BBTAG_IM.Core.Memory;

public sealed record CodeBlock(uint Address, byte[] Bytes)
{
    public bool Contains(uint address) => address >= Address && address - Address < Bytes.Length;
}

public sealed class CodeImage(IReadOnlyList<CodeBlock> blocks)
{
    private const byte CallOpcode = 0xE8;
    private const int CallLength = 5;

    public IReadOnlyList<uint> FindAll(BytePattern pattern) =>
        blocks
            .SelectMany(block => pattern.FindAll(block.Bytes).Select(offset => block.Address + (uint)offset))
            .ToArray();

    public uint FindSingle(BytePattern pattern, string name)
    {
        var matches = FindAll(pattern);

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw NotFound(name),
            _ => throw new GameLayoutException($"{name} was found {matches.Count} times in the game code. {GameLayoutException.UnsupportedVersion}"),
        };
    }

    public uint MostCalledTarget(BytePattern pattern, int callOffset, string name)
    {
        var targets = FindAll(pattern)
            .Select(match => match + (uint)callOffset)
            .Where(IsCall)
            .GroupBy(call => CallTarget(call, name))
            .OrderByDescending(group => group.Count())
            .ToArray();

        if (targets.Length == 0)
            throw NotFound(name);

        if (targets.Length > 1 && targets[0].Count() == targets[1].Count())
            throw new GameLayoutException($"{name} calls different functions equally often in the game code. {GameLayoutException.UnsupportedVersion}");

        return targets[0].Key;
    }

    public uint CallTarget(uint call, string name)
    {
        var callBytes = Slice(call, CallLength);

        if (callBytes.Length < CallLength || callBytes[0] != CallOpcode)
            throw new GameLayoutException($"{name} does not call its getter. {GameLayoutException.UnsupportedVersion}");

        return (uint)(call + CallLength + BinaryPrimitives.ReadInt32LittleEndian(callBytes[1..]));
    }

    private bool IsCall(uint address) => Slice(address, 1) is [CallOpcode];

    private static GameLayoutException NotFound(string name) =>
        new($"{name} was not found in the game code. {GameLayoutException.UnsupportedVersion}");

    public ReadOnlySpan<byte> Slice(uint address, int length)
    {
        var block = blocks.FirstOrDefault(candidate => candidate.Contains(address))
            ?? throw new GameLayoutException($"0x{address:X8} is outside the game code.");
        var offset = (int)(address - block.Address);

        return block.Bytes.AsSpan(offset, Math.Min(length, block.Bytes.Length - offset));
    }
}
