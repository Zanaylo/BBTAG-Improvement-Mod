using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Meter;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Training;
using BBTAG_IM.ImprovementMod.Graphics;
using BBTAG_IM.ImprovementMod.Menu;

namespace BBTAG_IM.ImprovementMod.Frames;

internal sealed class FrameMeterFeature
{
    private const string UnknownName = "?";

    private readonly GameView _game;
    private readonly CharacterRoster _roster;
    private readonly SettingsWatcher<ImprovementSettings> _settings;
    private readonly IMatchPause? _pause;
    private readonly HudRenderer _renderer;
    private readonly Lazy<GlyphSheet?> _font;
    private readonly ILog _log;
    private readonly FrameMeterRecorder _recorder = new(CharacterTrack.DefaultCapacity);
    private readonly CharacterFrame?[] _characters = new CharacterFrame?[MatchFrame.Slots];
    private readonly uint?[] _addresses = new uint?[MatchFrame.Slots];
    private readonly string[] _names = new string[MatchFrame.Slots];
    private readonly Dictionary<int, string> _knownNames = [];
    private readonly Action<HudScene, float, float> _compose;

    private bool _inMatch;
    private bool _reportedMissingFont;

    public FrameMeterFeature(GameView game, CharacterRoster roster, SettingsWatcher<ImprovementSettings> settings, IMatchPause? pause, HudRenderer renderer, Func<GlyphSheet?> loadFont, ILog log)
    {
        _game = game;
        _roster = roster;
        _settings = settings;
        _pause = pause;
        _renderer = renderer;
        _font = new Lazy<GlyphSheet?>(loadFont);
        _log = log;
        _compose = Compose;
        _recorder.Happened += OnMeterEvent;
    }

    public void OnPresent(nint device)
    {
        if (MenuInput.TakePresses(Hotkey.FrameMeter) % 2 == 1)
            Toggle();

        if (!_game.InTrainingMatch)
        {
            LeaveMatch();
            return;
        }

        _inMatch = true;
        Sample();

        if (_settings.Current.FrameMeter.Shown && Font() is { } font)
            _renderer.Draw(device, font, _compose);
    }

    private void Sample()
    {
        for (var slot = 0; slot < MatchFrame.Slots; slot++)
        {
            _addresses[slot] = _game.Character(slot);
            _characters[slot] = _addresses[slot] is { } address ? CharacterFrameReader.Read(_game.Memory, address) : null;
        }

        var projectiles = _game.EntityList is { } list ? ProjectileScan.Owners(_game.Memory, list, _addresses) : new bool[MatchFrame.Slots];
        _recorder.Record(new MatchFrame(_characters, projectiles));
    }

    private void Compose(HudScene scene, float width, float height)
    {
        for (var slot = 0; slot < MatchFrame.Slots; slot++)
            _names[slot] = NameOf(_game.CharacterIndex(slot));

        MeterLayout.Draw(scene, _recorder.View(_names), _settings.Current.FrameMeter.Look(), width, height, _font.Value!);
    }

    private string NameOf(int? index)
    {
        if (index is not { } known)
            return UnknownName;

        if (!_knownNames.TryGetValue(known, out var name))
        {
            name = _roster.Identify(known).Name.ToUpperInvariant();
            _knownNames[known] = name;
        }

        return name;
    }

    private void OnMeterEvent(MeterEvent happened)
    {
        if (_pause is { IsPaused: false } pause && AutoPausePolicy.ShouldPause(happened, _settings.Current.FrameMeter.AutoPause))
            pause.PauseNow();
    }

    private GlyphSheet? Font()
    {
        if (_font.Value is { } font)
            return font;

        if (!_reportedMissingFont)
        {
            _reportedMissingFont = true;
            SafeLog.Error(_log, $"The frame meter stays hidden: {GameFonts.Interface.FileName} could not be read from {GameFonts.Interface.Archive}.");
        }

        return null;
    }

    private void LeaveMatch()
    {
        if (!_inMatch)
            return;

        _inMatch = false;
        _recorder.Reset();
    }

    private void Toggle()
    {
        try
        {
            _settings.Change(options => options.FrameMeter.Shown = !options.FrameMeter.Shown);
            SafeLog.Info(_log, _settings.Current.FrameMeter.Shown ? "Frame meter on." : "Frame meter off.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SafeLog.Error(_log, $"The frame meter choice could not be saved: {exception.Message}");
        }
    }
}
