using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Launcher.Ui;

internal sealed class ModEntry(string name, string contents, bool isEnabled) : ObservableObject
{
    private bool _isEnabled = isEnabled;

    public event Action<ModEntry>? Toggled;

    public event Action<ModEntry, ModMove>? MoveRequested;

    public string Name => name;

    public string Contents => contents;

    public bool CanMoveUp { get; init; }

    public bool CanMoveDown { get; init; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
                Toggled?.Invoke(this);
        }
    }

    public void Move(ModMove move) => MoveRequested?.Invoke(this, move);
}
