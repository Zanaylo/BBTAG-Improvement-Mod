namespace BBTAG_IM.Launcher.Ui;

internal sealed class ModEntry(string name, string contents, bool isEnabled) : ObservableObject
{
    private bool _isEnabled = isEnabled;

    public event Action<ModEntry>? Toggled;

    public string Name => name;

    public string Contents => contents;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
                Toggled?.Invoke(this);
        }
    }
}
