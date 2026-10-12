using System.Collections.ObjectModel;

namespace BBTAG_IM.Launcher.Ui;

internal sealed class LauncherViewModel : ObservableObject, ILauncherView
{
    private const int MaximumMessages = 200;

    private readonly IReadOnlyList<ScriptNode> _scripts = [];
    private readonly IReadOnlyList<ModEntry> _mods = [];
    private readonly Dictionary<int, ScriptNode> _scriptsById = [];

    private string _status = "";
    private bool _canPlay;
    private bool _canChangeMods = true;
    private bool _frameLimiter;
    private bool _hitboxViewer = true;
    private CharacterPaletteChoices? _selectedPaletteCharacter;
    private bool _showCheatEngineWindow = true;

    public event Action<ScriptNode>? ScriptToggled;

    public event Action<ModEntry>? ModToggled;

    public event Action<bool>? FrameLimiterChanged;

    public event Action<bool>? HitboxViewerChanged;

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
                mod.Toggled += toggled => ModToggled?.Invoke(toggled);
        }
    }

    public bool CanChangeMods
    {
        get => _canChangeMods;
        set => SetProperty(ref _canChangeMods, value);
    }

    public string? TableName { get; init; }

    public string? ImprovementModName { get; init; }

    public string? PalettesFolder { get; init; }

    public IReadOnlyList<CharacterPaletteChoices> PaletteCharacters { get; init; } = [];

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

    public bool HasImprovementMod => ImprovementModName is not null;

    public bool HasPalettes => PaletteCharacters.Count > 0;

    public string PalettesCaption => (HasImprovementMod, HasPalettes) switch
    {
        (false, _) => "Put the BBTAG-Improvement-Mod folder inside the mods folder to use palettes.",
        (true, false) => $"No palettes yet. Put .impl palette files in {PalettesFolder}, one folder per character (for example palettes\\Aegis), then open the launcher again.",
        (true, true) => $"Pick a palette for any color of a character. It replaces that color from the next match, on your screen only. Palettes come from {PalettesFolder}.",
    };

    public CharacterPaletteChoices? SelectedPaletteCharacter
    {
        get => _selectedPaletteCharacter;
        set => SetProperty(ref _selectedPaletteCharacter, value);
    }

    public bool HitboxViewer
    {
        get => _hitboxViewer;
        set
        {
            if (SetProperty(ref _hitboxViewer, value))
                HitboxViewerChanged?.Invoke(value);
        }
    }

    public string ScriptsCaption => HasTable
        ? $"Scripts from {TableName}. Ticked scripts turn on when the game starts. The Core stays on because the overhaul needs it."
        : "No mod has a Cheat Engine table, so there are no scripts to choose.";

    public string ImprovementCaption => HasImprovementMod
        ? $"Options of {ImprovementModName}, saved inside that folder. The frame limiter changes from the next game start. The hitbox viewer changes right away, even during a match."
        : "Put the BBTAG-Improvement-Mod folder inside the mods folder to use these options.";

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

    public bool FrameLimiter
    {
        get => _frameLimiter;
        set
        {
            if (SetProperty(ref _frameLimiter, value))
                FrameLimiterChanged?.Invoke(value);
        }
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
        Messages.Add($"{DateTime.Now:HH:mm:ss}  {message}");

        while (Messages.Count > MaximumMessages)
            Messages.RemoveAt(0);
    }

    public void ShowImprovementOptions(bool frameLimiter, bool hitboxViewer)
    {
        _frameLimiter = frameLimiter;
        _hitboxViewer = hitboxViewer;
        OnPropertyChanged(nameof(FrameLimiter));
        OnPropertyChanged(nameof(HitboxViewer));
    }

    public void KeepMessagesOf(LauncherViewModel previous)
    {
        foreach (var message in previous.Messages)
            Messages.Add(message);

        Status = previous.Status;
    }

    public void ShowStatus(string message) => Status = message;

    public void ShowActiveScripts(IReadOnlySet<int> activeScripts)
    {
        foreach (var (id, node) in _scriptsById)
            node.Show(activeScripts.Contains(id));
    }
}
