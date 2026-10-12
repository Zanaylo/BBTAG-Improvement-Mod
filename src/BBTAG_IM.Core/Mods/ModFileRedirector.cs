namespace BBTAG_IM.Core.Mods;

public sealed class ModFileRedirector(ModFileIndex index, string gameDirectory)
{
    public const uint OpenExisting = 3;

    private const uint GenericWrite = 0x40000000;
    private const uint GenericAll = 0x10000000;
    private const uint FileWriteData = 0x00000002;
    private const uint FileAppendData = 0x00000004;
    private const uint FileWriteExtendedAttributes = 0x00000010;
    private const uint FileWriteAttributes = 0x00000100;
    private const uint Delete = 0x00010000;
    private const uint WriteDac = 0x00040000;
    private const uint WriteOwner = 0x00080000;
    private const uint WriteAccessMask = GenericWrite | GenericAll | FileWriteData | FileAppendData
        | FileWriteExtendedAttributes | FileWriteAttributes | Delete | WriteDac | WriteOwner;

    private long _redirections;

    public long Redirections => Interlocked.Read(ref _redirections);

    public string? RedirectOpen(string? path, uint desiredAccess, uint creationDisposition)
    {
        if (creationDisposition != OpenExisting || (desiredAccess & WriteAccessMask) != 0)
            return null;

        return RedirectLookup(path);
    }

    public string? RedirectLookup(string? path)
    {
        var key = GamePathKey.From(path, gameDirectory);

        if (key is null)
            return null;

        var redirected = index.Resolve(key);

        if (redirected is not null)
            Interlocked.Increment(ref _redirections);

        return redirected;
    }
}
