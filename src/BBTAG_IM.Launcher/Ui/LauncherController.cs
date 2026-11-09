using System.ComponentModel;
using System.Diagnostics;
using BBTAG_IM.Core.CheatEngine;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Mods.Updates;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Tables;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Launcher.Ui;

internal sealed class LauncherController
{
    private static readonly HttpClient GitHubClient = new() { Timeout = TimeSpan.FromMinutes(2) };

    private readonly string _launcherDirectory;
    private readonly LaunchOptions? _options;
    private readonly JsonFileStore<LauncherSettings> _settingsStore;
    private readonly LauncherSettings _settings;
    private IReadOnlyList<ModFolder> _allMods = [];
    private readonly List<int> _selection = [];
    private readonly ModSourceStore _sources = new(new PhysicalFileSystem());
    private readonly CancellationTokenSource _closing = new();

    private LaunchContext? _context;
    private GameSession? _session;
    private int _characterCheck;
    private bool _busy;

    public LauncherController(IReadOnlyList<string> arguments, string launcherDirectory)
    {
        var fileSystem = new PhysicalFileSystem();
        List<string> startupMessages = [];
        _launcherDirectory = launcherDirectory;
        _settingsStore = SettingsStores.Launcher(fileSystem, launcherDirectory);
        var settings = TryLoadSettings(startupMessages);
        _settings = settings ?? new LauncherSettings();
        _options = settings is null ? null : WithArchivesInstalled(ParseOptions(arguments, startupMessages), startupMessages);

        if (_options is not null)
            _allMods = _settings.InLoadOrder(DiscoverMods(_options, fileSystem, startupMessages));

        ViewModel = Load(startupMessages);
        CheckCharacters(_context);
    }

    public event Action<LauncherViewModel>? ViewModelReplaced;

    public LauncherViewModel ViewModel { get; private set; }

    private bool HasArchivesToUnpack => _options is not null && Archives().Pending().Count > 0;

    public async Task OpenedAsync()
    {
        UpdaterStarter.TryStartAfter(_launcherDirectory, Environment.ProcessId);

        if (HasArchivesToUnpack)
            await RefreshModsAsync();

        if (_settings.UpdateModsOnStart)
            await UpdateModsAsync();

        await PlayOnStartAsync();
    }

    public Task PlayOnStartAsync() => _settings.PlayOnStart ? PlayAsync() : Task.CompletedTask;

    public async Task PlayAsync()
    {
        if (_context is null || _session is not null || _busy)
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

    public void Close()
    {
        _closing.Cancel();
        _session?.Dispose();
    }

    public async Task RefreshModsAsync()
    {
        if (_options is null || _session is not null || _busy)
            return;

        await UnpackArchivesAsync();
        var before = _allMods.Select(mod => mod.Name).ToArray();
        List<string> messages = [];
        _allMods = _settings.InLoadOrder(DiscoverMods(_options, new PhysicalFileSystem(), messages));
        messages.Add(ModRefreshMessage.From(before, _allMods.Select(mod => mod.Name).ToArray()));
        Reload(messages);
    }

    private async Task UnpackArchivesAsync()
    {
        var archives = Archives();

        if (archives.Pending().Count == 0)
            return;

        IProgress<string> progress = new Progress<string>(ViewModel.Report);
        _busy = true;
        ViewModel.CanPlay = false;
        ViewModel.CanChangeMods = false;

        try
        {
            await Task.Run(() => archives.InstallPending(progress.Report));
        }
        finally
        {
            _busy = false;
        }
    }

    public Task UpdateModsAsync() =>
        _options is null ? Task.CompletedTask : RunUpdatesAsync(_sources.LinkedFolders(_settings.ModsDirectory(_launcherDirectory)));

    public async Task SaveLinkAsync()
    {
        if (ViewModel.SelectedMod is not { } entry || _allMods.FirstOrDefault(mod => mod.Name == entry.Name) is not { } mod)
            return;

        if (ImprovementModRelease.IsUpdatedByTheUpdater(mod.Name))
        {
            ViewModel.Report($"{mod.Name} is updated by {ImprovementModRelease.UpdaterFileName} from {ImprovementModRelease.Repository.Url}, so its link stays.");
            return;
        }

        var text = ViewModel.LinkText.Trim();

        if (text.Length == 0)
        {
            ChangeLink(() => _sources.Unlink(mod.RootPath), $"{mod.Name} no longer updates from GitHub.");
            return;
        }

        if (ParseLink(text) is not { } repository)
            return;

        if (ChangeLink(() => _sources.Link(mod.RootPath, repository), $"{mod.Name} now updates from {repository.Url}."))
            await RunUpdatesAsync([mod.RootPath]);
    }

    public async Task AddModFromLinkAsync()
    {
        if (_options is null || ParseLink(ViewModel.LinkText.Trim()) is not { } repository)
            return;

        var modsDirectory = _settings.ModsDirectory(_launcherDirectory);
        var folder = Path.Combine(modsDirectory, repository.Name);

        if (ChangeLink(() => _sources.AddFolder(modsDirectory, repository), $"{repository.Url} is downloading into the mods folder as {repository.Name}."))
            await RunUpdatesAsync([folder]);
    }

    private GitHubRepository? ParseLink(string text)
    {
        var repository = GitHubRepository.TryParse(text);

        if (repository is null)
            ViewModel.Report($"\"{text}\" is not a GitHub repository link. It looks like https://github.com/owner/mod.");

        return repository;
    }

    private bool ChangeLink(Action change, string message)
    {
        if (_session is not null || _busy)
            return false;

        try
        {
            change();
            Reload([message]);
            return true;
        }
        catch (Exception exception) when (IsUpdateError(exception))
        {
            ViewModel.Report(exception.Message);
            return false;
        }
    }

    private async Task RunUpdatesAsync(IReadOnlyList<string> folders)
    {
        if (_options is null || _session is not null || _busy || folders.Count == 0)
            return;

        _busy = true;
        ViewModel.CanPlay = false;
        ViewModel.CanChangeMods = false;
        IProgress<string> progress = new Progress<string>(ViewModel.ShowStatus);
        var updater = new ModUpdater(new GitHubApi(GitHubClient), new PhysicalFileSystem(), new ArchiveUnpacker());

        try
        {
            foreach (var folder in folders)
                await UpdateOneAsync(updater, folder, progress);
        }
        finally
        {
            _busy = false;
        }

        if (!_closing.IsCancellationRequested)
            await RefreshModsAsync();
    }

    private async Task UpdateOneAsync(ModUpdater updater, string folder, IProgress<string> progress)
    {
        try
        {
            var result = await Task.Run(() => updater.UpdateAsync(folder, progress, _closing.Token));
            ViewModel.Report(result.Message);
        }
        catch (OperationCanceledException) when (_closing.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception) when (IsUpdateError(exception))
        {
            ViewModel.Report($"{Path.GetFileName(folder)} was not updated: {exception.Message}");
        }
    }

    private static bool IsUpdateError(Exception exception) =>
        exception is ModUpdateException or GitHubException or ModArchiveException or IOException or UnauthorizedAccessException;

    private ModArchiveInstaller Archives() =>
        new(new PhysicalFileSystem(), new ArchiveUnpacker(), _settings.ModsDirectory(_launcherDirectory));

    private LaunchOptions? WithArchivesInstalled(LaunchOptions? options, List<string> messages)
    {
        if (options is null)
            return null;

        try
        {
            return Archives().WithArchivesInstalled(options);
        }
        catch (Exception exception) when (LaunchErrors.IsExpected(exception))
        {
            messages.Add(exception.Message);
            return options;
        }
    }

    private LauncherSettings? TryLoadSettings(List<string> messages)
    {
        try
        {
            return _settingsStore.Load();
        }
        catch (InvalidSettingsFileException exception)
        {
            messages.Add(exception.Message);
            return null;
        }
    }

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
            return ModSelection.Resolve(options, _settings.ModsDirectory(_launcherDirectory), fileSystem);
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
        viewModel.WriteLogs = _settings.WriteLogs;
        viewModel.PlayOnStart = _settings.PlayOnStart;
        viewModel.UpdateModsOnStart = _settings.UpdateModsOnStart;
        viewModel.ShowActiveScripts(_selection.ToHashSet());
        viewModel.CanPlay = _context is not null;
        viewModel.ScriptToggled += OnScriptToggled;
        viewModel.ModToggled += OnModToggled;
        viewModel.ModMoveRequested += OnModMoveRequested;
        viewModel.ShowCheatEngineWindowChanged += OnShowCheatEngineWindowChanged;
        viewModel.WriteLogsChanged += OnWriteLogsChanged;
        viewModel.PlayOnStartChanged += OnPlayOnStartChanged;
        viewModel.UpdateModsOnStartChanged += OnUpdateModsOnStartChanged;

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
        new(mod.Name, ModContents.Describe(mod, ModPlugin.IsPlugin), _settings.IsModEnabled(mod.Name), LinkOf(mod), LinkCaptionOf(mod))
        {
            CanMoveUp = position > 0,
            CanMoveDown = position < _allMods.Count - 1,
        };

    private void OnModToggled(ModEntry mod)
    {
        if (_session is not null)
            return;

        SaveModChange(() => _settings.EnableMod(mod.Name, mod.IsEnabled));
        Reload([mod.IsEnabled ? $"{mod.Name} is on." : $"{mod.Name} is off and will not load."]);
    }

    private void OnModMoveRequested(ModEntry mod, ModMove move)
    {
        if (_session is not null)
            return;

        SaveModChange(() => _settings.MoveMod(_allMods.Select(folder => folder.Name).ToArray(), mod.Name, move));
        _allMods = _settings.InLoadOrder(_allMods);
        Reload([move == ModMove.Up ? $"{mod.Name} moved up." : $"{mod.Name} moved down."]);
    }

    private void OnWriteLogsChanged(bool write) => SaveModChange(() => _settings.WriteLogs = write);

    private void OnPlayOnStartChanged(bool play) => SaveModChange(() => _settings.PlayOnStart = play);

    private void OnUpdateModsOnStartChanged(bool update) => SaveModChange(() => _settings.UpdateModsOnStart = update);

    private string LinkOf(ModFolder mod)
    {
        try
        {
            return _sources.Load(mod.RootPath)?.Repository ?? ImprovementModRelease.DefaultLinkOf(mod.Name) ?? "";
        }
        catch (ModUpdateException)
        {
            return "";
        }
    }

    private static string LinkCaptionOf(ModFolder mod) =>
        ImprovementModRelease.IsUpdatedByTheUpdater(mod.Name) ? $"Updates from {ImprovementModRelease.Repository.Url} through {ImprovementModRelease.UpdaterFileName}" : "";

    public void OpenLink(ModEntry mod)
    {
        if (!Uri.TryCreate(mod.Link, UriKind.Absolute, out var address) || address.Scheme != Uri.UriSchemeHttps)
        {
            ViewModel.Report($"{mod.Name} has no web link to open.");
            return;
        }

        try
        {
            using var browser = Process.Start(new ProcessStartInfo(address.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Win32Exception exception)
        {
            ViewModel.Report($"{address} could not be opened: {exception.Message}");
        }
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

    private void Reload(List<string> messages)
    {
        ViewModel = Load(messages, ViewModel);
        ViewModelReplaced?.Invoke(ViewModel);
        CheckCharacters(_context);
    }

    private async void CheckCharacters(LaunchContext? context)
    {
        if (context is null)
            return;

        var check = ++_characterCheck;

        try
        {
            var messages = await Task.Run(() => CharacterFileProblems(context));

            if (check == _characterCheck)
                messages.ToList().ForEach(ViewModel.AddMessage);
        }
        catch (Exception exception) when (LaunchErrors.IsExpected(exception))
        {
            ViewModel.AddMessage($"The character files could not be checked: {exception.Message}");
        }
    }

    private static IReadOnlyList<string> CharacterFileProblems(LaunchContext context)
    {
        var fileSystem = new PhysicalFileSystem();
        string[] roster = context.TablePath is { } tablePath ? [.. TableRoster.Codes(fileSystem.ReadAllText(tablePath)).Distinct()] : [];

        if (roster.Length == 0)
            return [];

        var reports = new ModCharacterAudit(fileSystem, context.GameDirectory).Run(context.Mods, roster);
        return CharacterFileMessages.From(reports);
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
