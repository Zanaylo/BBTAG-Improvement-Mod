using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Launcher.Ui;

internal sealed class ModEntry(string name, string contents, bool isEnabled, string link = "", string linkCaption = "") : ObservableObject
{
    private bool _isEnabled = isEnabled;

    public event Action<ModEntry>? Toggled;

    public event Action<ModEntry, ModMove>? MoveRequested;

    public string Name => name;

    public string Contents => contents;

    public string Link => link;

    public bool HasLink => link.Length > 0;

    public string LinkCaption => linkCaption.Length > 0 ? linkCaption : $"Updates from {link}";

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
