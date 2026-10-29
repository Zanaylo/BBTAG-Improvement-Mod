namespace BBTAG_IM.Native;

public sealed unsafe class CodeCave
{
    private readonly nint _block;

    private CodeCave(nint block) => _block = block;

    public uint Address => (uint)_block;

    public static CodeCave Reserve(int length, string purpose)
    {
        var block = Kernel32.VirtualAlloc(0, (nuint)length, Kernel32.MemoryCommit | Kernel32.MemoryReserve, Kernel32.PageExecuteReadWrite);

        if (block == 0)
            throw new PatchFailedException($"No memory could be reserved for the {purpose}.");

        return new CodeCave(block);
    }

    public void Put(int offset, ReadOnlySpan<byte> bytes) => bytes.CopyTo(new Span<byte>((void*)(_block + offset), bytes.Length));

    public static void Redirect(uint site, ReadOnlySpan<byte> jump)
    {
        ProtectedMemory.Write((nint)site, jump);
        Kernel32.FlushInstructionCache(Kernel32.GetCurrentProcess(), (nint)site, (nuint)jump.Length);
    }
}
