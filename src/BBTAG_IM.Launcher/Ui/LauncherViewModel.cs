using System.Collections.ObjectModel;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Launcher.Ui;

internal sealed class LauncherViewModel : ObservableObject, ILauncherView
{
    private const int MaximumMessages = 200;

    private RepeatedMessages _repeatedMessages = new();

    private readonly IReadOnlyList<ScriptNode> _scripts = [];
    private readonly IReadOnlyList<ModEntry> _mods = [];
    private readonly Dictionary<int, ScriptNode> _scriptsById = [];

    private string _status = "";
    private bool _canPlay;
    private bool _canChangeMods = true;
    private bool _showCheatEngineWindow = true;

    public event Action<ScriptNode>? ScriptToggled;

    public event Action<ModEntry>? ModToggled;

    public event Action<ModEntry, ModMove>? ModMoveRequested;

    public event Action<bool>? ShowCheatEngineWindowChanged;

    public string GameText { get; init; } = "";

    public string ExecutableText { get; init; } = "";

    public IReadOnlyList<ModEntry> Mods
    {
        get => _mods;
        init
        {
            _mods = value;

            foreach (var mod in value)
            {
                mod.Toggled += toggled => ModToggled?.Invoke(toggled);
                mod.MoveRequested += (moved, move) => ModMoveRequested?.Invoke(moved, move);
            }
        }
    }

    public bool CanChangeMods
    {
        get => _canChangeMods;
        set => SetProperty(ref _canChangeMods, value);
    }

    public string? TableName { get; init; }

    public ObservableCollection<string> Messages { get; } = [];

    public IReadOnlyList<ScriptNode> Scripts
    {
        get => _scripts;
        init
        {
            _scripts = value;

            foreach (var node in value.SelectMany(script => script.SelfAndDescendants()))
            {
                _scriptsById[node.Id] = node;
                node.Toggled += toggled => ScriptToggled?.Invoke(toggled);
            }
        }
    }

    public bool HasTable => TableName is not null;

    public string ScriptsCaption => HasTable
        ? $"Scripts from {TableName}. Ticked scripts turn on when the game starts. The Core stays on because the overhaul needs it."
        : "No mod has a Cheat Engine table, so there are no scripts to choose.";

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool CanPlay
    {
        get => _canPlay;
        set => SetProperty(ref _canPlay, value);
    }

    public bool ShowCheatEngineWindow
    {
        get => _showCheatEngineWindow;
        set
        {
            if (SetProperty(ref _showCheatEngineWindow, value))
                ShowCheatEngineWindowChanged?.Invoke(value);
        }
    }

    public void Report(string message)
    {
        Status = message;
        AddMessage(message);
    }

    public void AddMessage(string message)
    {
        var shown = _repeatedMessages.Show($"{DateTime.Now:HH:mm:ss}", message);

        if (shown.Replaces is { } earlierLine)
            Messages.Remove(earlierLine);

        Messages.Add(shown.Line);

        while (Messages.Count > MaximumMessages)
            Messages.RemoveAt(0);
    }

    public void KeepMessagesOf(LauncherViewModel previous)
    {
        foreach (var message in previous.Messages)
            Messages.Add(message);

        _repeatedMessages = previous._repeatedMessages;
        Status = previous.Status;
    }

    public void ShowStatus(string message) => Status = message;

    public void ShowActiveScripts(IReadOnlySet<int> activeScripts)
    {
        foreach (var (id, node) in _scriptsById)
            node.Show(activeScripts.Contains(id));
    }
}
