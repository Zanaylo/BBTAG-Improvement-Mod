namespace BBTAG_IM.Core.Memory;

public sealed record CodeBlock(uint Address, byte[] Bytes)
{
    public bool Contains(uint address) => address >= Address && address - Address < Bytes.Length;
}

public sealed class CodeImage(IReadOnlyList<CodeBlock> blocks)
{
    public uint FindSingle(BytePattern pattern, string name)
    {
        var matches = blocks
            .SelectMany(block => pattern.FindAll(block.Bytes).Select(offset => block.Address + (uint)offset))
            .ToArray();

        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new GameLayoutException($"{name} was not found in the game code. {GameLayoutException.UnsupportedVersion}"),
            _ => throw new GameLayoutException($"{name} was found {matches.Length} times in the game code. {GameLayoutException.UnsupportedVersion}"),
        };
    }

    public ReadOnlySpan<byte> Slice(uint address, int length)
    {
        var block = blocks.FirstOrDefault(candidate => candidate.Contains(address))
            ?? throw new GameLayoutException($"0x{address:X8} is outside the game code.");
        var offset = (int)(address - block.Address);

        return block.Bytes.AsSpan(offset, Math.Min(length, block.Bytes.Length - offset));
    }
}
