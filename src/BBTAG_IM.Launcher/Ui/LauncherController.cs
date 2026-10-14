using BBTAG_IM.Core.CheatEngine;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Launcher.Ui;

internal sealed class LauncherController
{
    private readonly string _launcherDirectory;
    private readonly LaunchOptions? _options;
    private readonly JsonFileStore<LauncherSettings> _settingsStore;
    private readonly LauncherSettings _settings;
    private IReadOnlyList<ModFolder> _allMods = [];
    private readonly List<int> _selection = [];

    private LaunchContext? _context;
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
            _allMods = _settings.InLoadOrder(DiscoverMods(_options, fileSystem, startupMessages));

        ViewModel = Load(startupMessages);
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
        _selection.Clear();
        _selection.AddRange(_context?.Table?.SelectionToEnable(_settings.SelectionFor(_context.TableName)) ?? []);

        var viewModel = CreateViewModel(_context);
        viewModel.ShowCheatEngineWindow = _settings.ShowCheatEngineWindow;
        viewModel.ShowActiveScripts(_selection.ToHashSet());
        viewModel.CanPlay = _context is not null;
        viewModel.ScriptToggled += OnScriptToggled;
        viewModel.ModToggled += OnModToggled;
        viewModel.ModMoveRequested += OnModMoveRequested;
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
            Mods = _allMods.Select(ToEntry).ToArray(),
            TableName = context?.Table is null ? null : context.TableName,
            Scripts = context?.Table is { } table ? ScriptNode.From(table) : [],
        };

    private ModEntry ToEntry(ModFolder mod, int position) =>
        new(mod.Name, ModContents.Describe(mod, ModPlugin.IsPlugin), _settings.IsModEnabled(mod.Name))
        {
            CanMoveUp = position > 0,
            CanMoveDown = position < _allMods.Count - 1,
        };

    private void OnModToggled(ModEntry mod)
    {
        if (_session is not null)
            return;

        SaveModChange(() => _settings.EnableMod(mod.Name, mod.IsEnabled));
        Reload(mod.IsEnabled ? $"{mod.Name} is on." : $"{mod.Name} is off and will not load.");
    }

    private void OnModMoveRequested(ModEntry mod, ModMove move)
    {
        if (_session is not null)
            return;

        SaveModChange(() => _settings.MoveMod(_allMods.Select(folder => folder.Name).ToArray(), mod.Name, move));
        _allMods = _settings.InLoadOrder(_allMods);
        Reload(move == ModMove.Up ? $"{mod.Name} moved up." : $"{mod.Name} moved down.");
    }

    private void SaveModChange(Action change)
    {
        try
        {
            change();
            _settingsStore.Save(_settings);
        }
        catch (Exception exception) when (LaunchErrors.IsSettingsWriteError(exception))
        {
            ViewModel.Report(exception.Message);
        }
    }

    private void Reload(string message)
    {
        ViewModel = Load([message], ViewModel);
        ViewModelReplaced?.Invoke(ViewModel);
    }

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

    private void Remember(int scriptId, bool turnOn)
    {
        _selection.Remove(scriptId);

        if (turnOn)
            _selection.Add(scriptId);

        _settings.Remember(_context!.TableName, _selection);
        _settingsStore.Save(_settings);
    }
}
