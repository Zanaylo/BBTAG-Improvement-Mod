namespace BBTAG_IM.Core.Palettes;

public sealed class EntrySelection
{
    private readonly SortedSet<int> _entries = [];

    private int _anchor;

    public EntrySelection(int entry) => Pick(entry);

    public int Focus { get; private set; }

    public IReadOnlyCollection<int> Entries => _entries;

    public int Count => _entries.Count;

    public bool Contains(int entry) => _entries.Contains(entry);

    public void Pick(int entry)
    {
        _entries.Clear();
        _entries.Add(entry);
        Focus = entry;
        _anchor = entry;
    }

    public void Toggle(int entry)
    {
        if (!_entries.Contains(entry))
        {
            _entries.Add(entry);
            Focus = entry;
            _anchor = entry;
            return;
        }

        if (_entries.Count == 1)
            return;

        _entries.Remove(entry);

        if (Focus == entry)
            Focus = _entries.Min;
    }

    public void ExtendTo(int entry)
    {
        _entries.Clear();

        for (var selected = Math.Min(_anchor, entry); selected <= Math.Max(_anchor, entry); selected++)
            _entries.Add(selected);

        Focus = entry;
    }
}
