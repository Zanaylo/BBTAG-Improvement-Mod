using Avalonia.Threading;
using BBTAG_IM.Core.CheatEngine;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Launcher.Ui;

internal sealed class LauncherController
{
    private static readonly TimeSpan OptionsCheckInterval = TimeSpan.FromSeconds(1);

    private readonly string _launcherDirectory;
    private readonly LaunchOptions? _options;
    private readonly JsonFileStore<LauncherSettings> _settingsStore;
    private readonly LauncherSettings _settings;
    private readonly IReadOnlyList<ModFolder> _allMods = [];
    private readonly List<int> _selection = [];

    private LaunchContext? _context;
    private SettingsWatcher<ImprovementSettings>? _improvement;
    private GameSession? _session;

    public LauncherController(IReadOnlyList<string> arguments, string launcherDirectory)
    {
        var fileSystem = new PhysicalFileSystem();
        List<string> startupMessages = [];
        _launcherDirectory = launcherDirectory;
        _settingsStore = SettingsStores.Launcher(fileSystem, launcherDirectory);
        _settings = _settingsStore.Load();
        _options = ParseOptions(arguments, startupMessages);

        if (_options is not null)
            _allMods = DiscoverMods(_options, fileSystem, startupMessages);

        ViewModel = Load(startupMessages);

        var optionsCheck = new DispatcherTimer { Interval = OptionsCheckInterval };
        optionsCheck.Tick += (_, _) => ShowOptionsChangedElsewhere();
        optionsCheck.Start();
    }

    public event Action<LauncherViewModel>? ViewModelReplaced;

    public LauncherViewModel ViewModel { get; private set; }

    public async Task PlayAsync()
    {
        if (_context is null || _session is not null)
            return;

        ViewModel.CanPlay = false;
        ViewModel.CanChangeMods = false;
        _session = new GameSession(_context, _settings, ViewModel);

        try
        {
            await _session.RunAsync(() => _selection.ToArray());
        }
        catch (Exception exception)
        {
            ViewModel.Report(LaunchErrors.IsExpected(exception) ? exception.Message : exception.ToString());
        }
        finally
        {
            _session.Dispose();
            _session = null;
            ViewModel.ShowActiveScripts(_selection.ToHashSet());
            ViewModel.CanPlay = true;
            ViewModel.CanChangeMods = true;
        }
    }

    public void Close() => _session?.Dispose();

    private static LaunchOptions? ParseOptions(IReadOnlyList<string> arguments, List<string> messages)
    {
        try
        {
            var options = LaunchOptions.Parse(arguments);

            if (options.ShowHelp)
                messages.Add(Usage.Text);

            return options;
        }
        catch (LaunchOptionsException exception)
        {
            messages.Add(exception.Message);
            return null;
        }
    }

    private IReadOnlyList<ModFolder> DiscoverMods(LaunchOptions options, IFileSystem fileSystem, List<string> messages)
    {
        try
        {
            return ModSelection.Resolve(options, _launcherDirectory, fileSystem);
        }
        catch (Exception exception) when (LaunchErrors.IsExpected(exception))
        {
            messages.Add(exception.Message);
            return [];
        }
    }

    private LauncherViewModel Load(List<string> messages, LauncherViewModel? previous = null)
    {
        _context = ResolveContext(messages);
        _improvement = _context?.ImprovementStore is { } store ? new SettingsWatcher<ImprovementSettings>(store, new PhysicalFileSystem()) : null;
        var improvement = _improvement?.Current ?? new ImprovementSettings();
        _selection.Clear();
        _selection.AddRange(_context?.Table?.SelectionToEnable(_settings.SelectionFor(_context.TableName)) ?? []);

        var viewModel = CreateViewModel(_context);
        viewModel.FrameLimiter = improvement.FrameLimiter;
        viewModel.HitboxViewer = improvement.HitboxViewer;
        viewModel.SelectedPaletteCharacter = viewModel.PaletteCharacters.FirstOrDefault();
        viewModel.ShowCheatEngineWindow = _settings.ShowCheatEngineWindow;
        viewModel.ShowActiveScripts(_selection.ToHashSet());
        viewModel.CanPlay = _context is not null;
        viewModel.ScriptToggled += OnScriptToggled;
        viewModel.ModToggled += OnModToggled;
        viewModel.FrameLimiterChanged += OnFrameLimiterChanged;
        viewModel.HitboxViewerChanged += OnHitboxViewerChanged;
        viewModel.ShowCheatEngineWindowChanged += OnShowCheatEngineWindowChanged;

        if (previous is not null)
            viewModel.KeepMessagesOf(previous);

        messages.ForEach(viewModel.Report);

        if (_context is not null)
            viewModel.Report("Ready. Press Play to start the game.");

        return viewModel;
    }

    private LaunchContext? ResolveContext(List<string> messages)
    {
        if (_options is null)
            return null;

        try
        {
            return LaunchContext.Resolve(_options, _launcherDirectory, _settings.EnabledOf(_allMods));
        }
        catch (Exception exception) when (LaunchErrors.IsExpected(exception))
        {
            messages.Add(exception.Message);
            return null;
        }
    }

    private LauncherViewModel CreateViewModel(LaunchContext? context) =>
        new()
        {
            GameText = context is null ? "" : $"Game: {context.GameDirectory}",
            ExecutableText = context is null ? "" : $"Runs: {context.Target.ExecutablePath}",
            Mods = _allMods.Select(mod => new ModEntry(mod.Name, ModContents.Describe(mod, ModPlugin.IsPlugin), _settings.IsModEnabled(mod.Name))).ToArray(),
            TableName = context?.Table is null ? null : context.TableName,
            ImprovementModName = context?.ImprovementMod?.Name,
            PalettesFolder = context?.Palettes?.Folder,
            PaletteCharacters = context?.Palettes is { } palettes ? CreatePaletteCharacters(palettes) : [],
            Scripts = context?.Table is { } table ? ScriptNode.From(table) : [],
        };

    private IReadOnlyList<CharacterPaletteChoices> CreatePaletteCharacters(PaletteLibrary library) =>
        library.CharactersWithPalettes()
            .Select(character => new CharacterPaletteChoices(character, () => CreatePaletteColors(library, character)))
            .ToArray();

    private IReadOnlyList<PaletteColorSlot> CreatePaletteColors(PaletteLibrary library, string character)
    {
        var palettes = library.Load(character);
        palettes.Problems.ToList().ForEach(ViewModel.Report);
        PaletteOption[] options = [PaletteOption.GameColors, .. palettes.Palettes.Select(palette => new PaletteOption(palette.Name, palette))];

        return Enumerable.Range(1, PaletteTable.StockColors)
            .Select(color => CreatePaletteColor(character, color, options))
            .ToArray();
    }

    private PaletteColorSlot CreatePaletteColor(string character, int color, IReadOnlyList<PaletteOption> options)
    {
        var chosen = _improvement?.Current.PaletteFor(character, color);
        var selected = OptionFor(options, chosen);

        if (chosen is not null && selected == PaletteOption.GameColors)
            ViewModel.Report($"{character} color {color} was set to the palette {chosen}, which is no longer in the palettes folder.");

        var slot = new PaletteColorSlot(color, options, selected);
        slot.Changed += changed => OnPaletteChosen(character, changed);
        return slot;
    }

    private void ShowOptionsChangedElsewhere()
    {
        if (_improvement is null || !RefreshImprovement(_improvement))
            return;

        var options = _improvement.Current;
        ViewModel.ShowImprovementOptions(options.FrameLimiter, options.HitboxViewer);

        foreach (var character in ViewModel.PaletteCharacters)
        {
            foreach (var slot in character.LoadedColors)
                slot.Show(OptionFor(slot.Options, options.PaletteFor(character.Name, slot.Number)));
        }

        ViewModel.Report("The options were changed in the game menu.");
    }

    private static bool RefreshImprovement(SettingsWatcher<ImprovementSettings> improvement)
    {
        try
        {
            return improvement.Refresh();
        }
        catch (Exception exception) when (LaunchErrors.IsSettingsWriteError(exception))
        {
            return false;
        }
    }

    private static PaletteOption OptionFor(IReadOnlyList<PaletteOption> options, string? paletteName) =>
        options.FirstOrDefault(option => option.Palette?.Name.Equals(paletteName, StringComparison.OrdinalIgnoreCase) == true)
            ?? PaletteOption.GameColors;

    private void OnModToggled(ModEntry mod)
    {
        if (_session is not null)
            return;

        try
        {
            _settings.EnableMod(mod.Name, mod.IsEnabled);
            _settingsStore.Save(_settings);
        }
        catch (Exception exception) when (LaunchErrors.IsSettingsWriteError(exception))
        {
            ViewModel.Report(exception.Message);
        }

        ViewModel = Load([mod.IsEnabled ? $"{mod.Name} is on." : $"{mod.Name} is off and will not load."], ViewModel);
        ViewModelReplaced?.Invoke(ViewModel);
    }

    private void OnPaletteChosen(string character, PaletteColorSlot slot)
    {
        var palette = slot.Selected.Palette?.Name;

        SaveImprovementSettings(
            settings => settings.ChoosePalette(character, slot.Number, palette),
            palette is null
                ? $"{character} color {slot.Number} uses the game colors from the next match."
                : $"{character} color {slot.Number} uses the palette {palette} from the next match.");
    }

    private void OnHitboxViewerChanged(bool turnOn) =>
        SaveImprovementSettings(
            settings => settings.HitboxViewer = turnOn,
            turnOn
                ? "Hitboxes show during training matches."
                : "Hitboxes stay hidden.");

    private async void OnScriptToggled(ScriptNode script)
    {
        try
        {
            Remember(script.Id, script.IsChecked);

            if (_session is not null)
                await _session.ToggleAsync(script.Id, script.IsChecked);
        }
        catch (Exception exception) when (exception is BridgeException || LaunchErrors.IsSettingsWriteError(exception))
        {
            ViewModel.Report(exception.Message);
        }
    }

    private async void OnShowCheatEngineWindowChanged(bool visible)
    {
        try
        {
            _settings.ShowCheatEngineWindow = visible;
            _settingsStore.Save(_settings);

            if (_session is not null)
                await _session.ShowWindowAsync(visible);
        }
        catch (Exception exception) when (exception is BridgeException || LaunchErrors.IsSettingsWriteError(exception))
        {
            ViewModel.Report(exception.Message);
        }
    }

    private void OnFrameLimiterChanged(bool turnOn) =>
        SaveImprovementSettings(
            settings => settings.FrameLimiter = turnOn,
            turnOn
                ? "The frame limiter turns V-Sync off and holds 60 fps from the next game start."
                : "The game uses its native V-Sync setting again from the next game start.");

    private void SaveImprovementSettings(Action<ImprovementSettings> change, string confirmation)
    {
        if (_improvement is null)
            return;

        try
        {
            _improvement.Change(change);
            ViewModel.Report(confirmation);
        }
        catch (Exception exception) when (LaunchErrors.IsSettingsWriteError(exception))
        {
            ViewModel.Report(exception.Message);
        }
    }

    private void Remember(int scriptId, bool turnOn)
    {
        _selection.Remove(scriptId);

        if (turnOn)
            _selection.Add(scriptId);

        _settings.Remember(_context!.TableName, _selection);
        _settingsStore.Save(_settings);
    }
}
