using System.Numerics;
using BBTAG_IM.Core.Online;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class ImprovementMenu(MenuSettings settings, PlayerCount playerCount, IReadOnlyList<IMenuSection> sections, IReadOnlyList<IMenuWindow> windows)
{
    private const string Title = "BBTAG Improvement Mod";
    private const string PlayerCountLabel = "Current online players:";
    private const float WidthInFontSizes = 32;
    private const float NarrowestInFontSizes = 20;
    private const float LowestInFontSizes = 10;
    private const float FirstShareOfScreenHeight = 0.7f;

    private static readonly Vector2 FirstPosition = new(0.05f, 0.08f);
    private static readonly Vector4 PlayerCountColor = new(0, 1, 0, 1);

    private int? _shownPlayers;
    private string _shownPlayersText = "";

    public void Reopened()
    {
        settings.ClearStatus();

        foreach (var section in sections)
            section.Reopened();
    }

    public bool Draw(Vector2 displaySize)
    {
        var open = true;
        var font = ImGui.GetFontSize();

        ImGui.SetNextWindowPos(displaySize * FirstPosition, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(font * WidthInFontSizes, displaySize.Y * FirstShareOfScreenHeight), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(font * NarrowestInFontSizes, font * LowestInFontSizes), new Vector2(float.MaxValue));

        if (ImGui.Begin(Title, ref open))
            DrawContents();

        ImGui.End();

        foreach (var window in windows)
            window.Draw(displaySize);

        return open;
    }

    private void DrawContents()
    {
        ImGui.TextDisabled($"{MenuInput.ToggleKeyName} opens and closes this menu.");
        DrawPlayerCount();

        if (settings.Status.Length > 0)
            MenuText.Wrapped(settings.Status);

        if (!ImGui.BeginTabBar("sections", ImGuiTabBarFlags.FittingPolicyResizeDown))
            return;

        foreach (var section in sections)
            DrawSection(section);

        ImGui.EndTabBar();
    }

    private void DrawPlayerCount()
    {
        ImGui.TextUnformatted(PlayerCountLabel);
        ImGui.SameLine();

        if (playerCount.Players is not { } players)
        {
            ImGui.TextDisabled(playerCount.Status);
            return;
        }

        if (players != _shownPlayers)
        {
            _shownPlayers = players;
            _shownPlayersText = players.ToString();
        }

        ImGui.TextColored(PlayerCountColor, _shownPlayersText);
    }

    private static void DrawSection(IMenuSection section)
    {
        if (!ImGui.BeginTabItem(section.Title))
            return;

        ImGui.Spacing();
        section.Draw();
        ImGui.EndTabItem();
    }
}
