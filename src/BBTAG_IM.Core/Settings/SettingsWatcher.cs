using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Settings;

public sealed class SettingsWatcher<T> where T : class, new()
{
    private readonly JsonFileStore<T> _store;
    private readonly IFileSystem _fileSystem;

    private bool _loaded;
    private DateTime? _loadedWrite;

    public SettingsWatcher(JsonFileStore<T> store, IFileSystem fileSystem)
    {
        _store = store;
        _fileSystem = fileSystem;
        Refresh();
    }

    public T Current { get; private set; } = new();

    public void Change(Action<T> change)
    {
        change(Current);
        _store.Save(Current);
        _loadedWrite = LastWrite();
    }

    public bool Refresh()
    {
        var lastWrite = LastWrite();

        if (_loaded && lastWrite == _loadedWrite)
            return false;

        if (_store.TryLoad() is not { } settings)
            return false;

        _loaded = true;
        _loadedWrite = lastWrite;
        Current = settings;
        return true;
    }

    private DateTime? LastWrite() => _fileSystem.FileExists(_store.Path) ? _fileSystem.LastWriteTimeUtc(_store.Path) : null;
}
