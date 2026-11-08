using System.Numerics;
using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Hantei.Games;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Porting;
using BBTAG_IM.Core.Roster;
using BBTAG_IM.ImprovementMod.Dev;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed record PortFolders(string GameData, string Mods, string Database);

internal sealed class CharacterPortPanel(IFileSystem files, FileDialog dialog, PortRunner runner, PortFolders folders) : IDevPanel
{
    private const string DefaultSeries = "Others";
    private const string GameDialogTitle = "Choose a French-Bread game";
    private const string GameDialogFilter = "French-Bread games\0uni2.exe;MBTL.exe;BaseData.HA6\0\0";
    private const int VisibleRows = 10;

    private static readonly string[] SeriesNames = Enum.GetNames<Series>();

    private HanteiGame? _game;
    private IReadOnlyList<HanteiCharacter> _characters = [];
    private string _series = DefaultSeries;
    private string? _building;
    private string _notice = string.Empty;

    public void Draw()
    {
        ImGui.SeparatorText("French-Bread characters");
        MenuText.Wrapped("Add a character of UNDER NIGHT IN-BIRTH II or MELTY BLOOD: TYPE LUMINA to the roster, using the files of the game you have installed. It takes the sprites, palettes, frame timing, hitboxes, normals and specials of that game. Common actions such as walking, blocking and tagging follow a BBTAG template.");
        ImGui.Spacing();

        TakeChosenGame();
        TakeOutcome();
        DrawGameChoice();

        if (_game is not null)
            DrawGame(_game);

        if (_notice.Length > 0)
            MenuText.Wrapped(_notice);
    }

    private void DrawGameChoice()
    {
        ImGui.BeginDisabled(dialog.IsOpen || runner.IsRunning);

        if (ImGui.Button("Choose a game"))
            dialog.ChooseGame(GameDialogTitle, GameDialogFilter);

        ImGui.EndDisabled();
        MenuText.Tooltip("Pick uni2.exe, MBTL.exe, or the BaseData.HA6 of a folder with unpacked files.");

        if (_game is null)
            return;

        ImGui.SameLine();
        ImGui.TextUnformatted(_game.Title);
    }

    private void DrawGame(HanteiGame game)
    {
        DrawSeries();

        if (_characters.Count == 0)
        {
            MenuText.Muted("No character with all its files was found.");
            return;
        }

        if (runner.IsRunning)
        {
            MenuText.Muted($"Building {_building}. The match can keep running.");
            return;
        }

        DrawCharacters(game);
    }

    private void DrawSeries()
    {
        ImGui.BeginDisabled(runner.IsRunning);

        if (ImGui.BeginCombo("Row on character select", _series))
        {
            foreach (var series in SeriesNames)
            {
                var selected = series == _series;

                if (ImGui.Selectable(series, selected))
                    _series = series;

                MenuCombo.KeepSelectedInView(selected);
            }

            ImGui.EndCombo();
        }

        ImGui.EndDisabled();
    }

    private void DrawCharacters(HanteiGame game)
    {
        var height = Math.Min(_characters.Count, VisibleRows) * ImGui.GetFrameHeightWithSpacing();

        if (ImGui.BeginChild("portCharacters", new Vector2(0, height), ImGuiChildFlags.Borders))
        {
            foreach (var character in _characters)
                DrawCharacter(new PortJob(game, character, _series));
        }

        ImGui.EndChild();
    }

    private void DrawCharacter(PortJob job)
    {
        ImGui.PushID(job.Character.Code);

        if (ImGui.SmallButton(files.DirectoryExists(Path.Combine(folders.Mods, job.ModName)) ? "Port again" : "Port"))
            Start(job);

        MenuText.Tooltip($"Adds {job.Character.Name} to character select as mods\\{job.ModName}.");
        ImGui.SameLine();
        ImGui.TextUnformatted(job.Character.Name);
        ImGui.PopID();
    }

    private void Start(PortJob job)
    {
        _building = job.Character.Name;
        _notice = string.Empty;
        runner.Start(job, job.Arguments(folders.GameData, folders.Mods, folders.Database));
    }

    private void TakeChosenGame()
    {
        if (dialog.Take() is not { Paths: [var path, ..] })
            return;

        try
        {
            _game = HanteiGames.Open(path, files);
            _characters = _game.Characters();
            _notice = string.Empty;
        }
        catch (Exception exception) when (exception is InvalidHanteiDataException or InvalidPeImageException or IOException or UnauthorizedAccessException)
        {
            _game = null;
            _characters = [];
            _notice = exception.Message;
        }
    }

    private void TakeOutcome()
    {
        if (runner.Take() is not { } outcome)
            return;

        _notice = outcome.Error is null
            ? $"{outcome.Job.Character.Name} is in mods\\{outcome.Job.ModName}. Close the game and press Play in the Mod Loader with that mod ticked: the character joins the {outcome.Job.Series} row."
            : $"{outcome.Job.Character.Name} could not be built: {outcome.Error}";
    }
}
