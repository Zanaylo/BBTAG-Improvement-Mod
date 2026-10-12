using System.Collections.Concurrent;

namespace BBTAG_IM.Core.Palettes;

public sealed class PaletteCatalog(PaletteLibrary library, Action<Action> runInBackground)
{
    private readonly ConcurrentDictionary<string, CharacterPalettes> _loaded = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ReadState> _reading = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _readingLock = new();

    public PaletteLibrary Library => library;

    public CharacterPalettes Load(string folder) => Loaded(folder) ?? Read(folder);

    public CharacterPalettes? Loaded(string folder) => _loaded.GetValueOrDefault(folder);

    public CharacterPalettes? LoadInBackground(string folder)
    {
        if (Loaded(folder) is { } palettes)
            return palettes;

        Reload(folder);
        return null;
    }

    public void Reload(string folder)
    {
        if (QueueRead(folder))
            runInBackground(() => ReadUntilCurrent(folder));
    }

    public void ReloadAll()
    {
        foreach (var folder in _loaded.Keys)
            Reload(folder);
    }

    private bool QueueRead(string folder)
    {
        lock (_readingLock)
        {
            if (!_reading.TryGetValue(folder, out var state))
            {
                _reading[folder] = ReadState.Queued;
                return true;
            }

            if (state == ReadState.Reading)
                _reading[folder] = ReadState.ReadAgain;

            return false;
        }
    }

    private void ReadUntilCurrent(string folder)
    {
        do
        {
            StartReading(folder);
            Read(folder);
        }
        while (MustReadAgain(folder));
    }

    private void StartReading(string folder)
    {
        lock (_readingLock)
            _reading[folder] = ReadState.Reading;
    }

    private bool MustReadAgain(string folder)
    {
        lock (_readingLock)
        {
            if (_reading[folder] == ReadState.ReadAgain)
                return true;

            _reading.Remove(folder);
            return false;
        }
    }

    private CharacterPalettes Read(string folder) => _loaded[folder] = library.Load(folder);

    private enum ReadState
    {
        Queued,
        Reading,
        ReadAgain,
    }
}
