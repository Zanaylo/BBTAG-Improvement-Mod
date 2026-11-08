namespace BBTAG_IM.Core.Mods;

public sealed class ModFileRedirector(ModFileIndex index, string gameDirectory, string executableDirectory, DevOverlay? dev = null, CharacterFileAliases? aliases = null)
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

    private readonly bool _runsFromModFolder = !Path.TrimEndingDirectorySeparator(executableDirectory)
        .Equals(Path.TrimEndingDirectorySeparator(gameDirectory), StringComparison.OrdinalIgnoreCase);

    private long _redirections;

    public long Redirections => Interlocked.Read(ref _redirections);

    public static bool ReadsExistingFile(uint desiredAccess, uint creationDisposition) =>
        creationDisposition == OpenExisting && (desiredAccess & WriteAccessMask) == 0;

    public string? RedirectOpen(string? path, uint desiredAccess, uint creationDisposition) =>
        Counted(Redirect(path, ReadsExistingFile(desiredAccess, creationDisposition)));

    public string? RedirectLookup(string? path) => Counted(Redirect(path, readsExistingFile: true));

    public string? AnchorToGame(string? path) =>
        string.IsNullOrEmpty(path) || Path.IsPathRooted(path) ? null : Path.GetFullPath(path, gameDirectory);

    public string? GameFileKey(string? path) =>
        (_runsFromModFolder ? KeyBesideExecutable(path) : null) ?? GamePathKey.From(path, gameDirectory);

    private string? Redirect(string? path, bool readsExistingFile)
    {
        var besideExecutable = _runsFromModFolder ? KeyBesideExecutable(path) : null;

        if (besideExecutable is not null)
            return (readsExistingFile ? Resolve(besideExecutable) : null) ?? Path.Join(gameDirectory, besideExecutable);

        var key = GamePathKey.From(path, gameDirectory);
        return readsExistingFile && key is not null ? Resolve(key) : null;
    }

    public string? ResolveBelowDev(string key) => index.Resolve(key) ?? (File.Exists(Path.Join(gameDirectory, key)) ? Path.Join(gameDirectory, key) : null);

    private string? Resolve(string key) => dev?.Resolve(key) ?? index.Resolve(key) ?? ResolveAlias(key);

    private string? ResolveAlias(string key) =>
        !File.Exists(Path.Join(gameDirectory, key)) && aliases?.AliasOf(key) is { } alias ? ResolveBelowDev(alias) : null;

    private string? KeyBesideExecutable(string? path) =>
        Path.IsPathFullyQualified(path ?? "") ? GamePathKey.From(path, executableDirectory) : null;

    private string? Counted(string? redirected)
    {
        if (redirected is not null)
            Interlocked.Increment(ref _redirections);

        return redirected;
    }
}
