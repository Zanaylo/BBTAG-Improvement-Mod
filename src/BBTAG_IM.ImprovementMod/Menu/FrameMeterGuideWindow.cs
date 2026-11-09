using System.Numerics;
using BBTAG_IM.Core.Meter;
using BBTAG_IM.Core.Overlay;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class FrameMeterGuideWindow : IMenuWindow
{
    private const string Title = "Frame meter guide";

    private static readonly Vector2 FirstPosition = new(0.55f, 0.08f);

    private static readonly (FrameKind Kind, string Name, string Meaning)[] Kinds =
    [
        (FrameKind.Idle, "Free", "Can act."),
        (FrameKind.Movement, "Movement", "Walk, dash or jump."),
        (FrameKind.Startup, "Startup", "Before the first hit."),
        (FrameKind.Active, "Active", "Hitbox out."),
        (FrameKind.Recovery, "Recovery", "After the last hit."),
        (FrameKind.Hitstun, "Hitstun", "Getting hit."),
        (FrameKind.Blockstun, "Blockstun", "Blocking."),
        (FrameKind.Tech, "Tech", "Getting up."),
        (FrameKind.Leaving, "Partner leaving", "Assist going back."),
    ];

    private static readonly (Invulnerability Covered, string Name)[] Strip =
    [
        (Invulnerability.Full, "Fully invincible"),
        (Invulnerability.Strike, "Strike invincible"),
        (Invulnerability.Throw, "Throw invincible"),
        (Invulnerability.Projectile, "Projectile invincible"),
    ];

    private bool _isOpen;

    public void Toggle() => _isOpen = !_isOpen;

    public void Draw(Vector2 displaySize)
    {
        if (!_isOpen)
            return;

        if (FittedWindow.Begin(Title, ref _isOpen, displaySize * FirstPosition))
            DrawContents();

        ImGui.End();
    }

    private static void DrawContents()
    {
        foreach (var (kind, name, meaning) in Kinds)
            Swatch(MeterColors.Of(kind), name, meaning);

        ImGui.Separator();
        MenuText.Muted("Outer strip");

        foreach (var (covered, name) in Strip)
            Swatch(MeterColors.OfInvulnerability(covered), name, "");

        Swatch(MeterColors.Armor, "Armor", "");
        ImGui.Separator();
        Swatch(MeterColors.HitStart, "White edge", "Contact.");
        Swatch(MeterColors.Projectile, "Orange dot", "Projectile hitbox out.");
        ImGui.Separator();
        MenuText.Wrapped("ADVANTAGE: frames this team acts first.");
        MenuText.Wrapped("GAP: free frames between two blocked hits.");
        MenuText.Wrapped("TOTAL: first startup frame to first free frame.");
    }

    private static void Swatch(uint color, string name, string meaning)
    {
        var size = new Vector2(ImGui.GetFontSize());
        ImGui.ColorButton($"##{name}", ImGui.ColorConvertU32ToFloat4(OverlayColors.SwapRedAndBlue(color)), ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker, size);
        ImGui.SameLine();
        MenuText.Wrapped(meaning.Length > 0 ? $"{name}: {meaning}" : name);
    }
}
