using System.Numerics;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal static class MenuStyle
{
    private const float WindowRounding = 4f;
    private const float FrameRounding = 3f;
    private const float PopupRounding = 4f;
    private const float WindowBorder = 1f;
    private const float NoBorder = 0f;
    private const float SeparatorTextBorder = 2f;

    private static readonly Vector2 WindowPadding = new(12f, 10f);
    private static readonly Vector2 FramePadding = new(8f, 4f);
    private static readonly Vector2 ItemSpacing = new(8f, 6f);
    private static readonly Vector2 ItemInnerSpacing = new(6f, 4f);
    private static readonly Vector2 TitleOnTheLeft = new(0f, 0.5f);

    private static readonly Vector4 Accent = new(0.10f, 0.42f, 0.86f, 1.00f);
    private static readonly Vector4 AccentBright = new(0.36f, 0.64f, 1.00f, 1.00f);
    private static readonly Vector4 AccentDark = new(0.04f, 0.15f, 0.36f, 1.00f);
    private static readonly Vector4 AccentHover = new(0.07f, 0.29f, 0.62f, 1.00f);
    private static readonly Vector4 Text = new(0.96f, 0.96f, 0.98f, 1.00f);
    private static readonly Vector4 DisabledText = new(0.50f, 0.52f, 0.58f, 1.00f);
    private static readonly Vector4 Window = new(0.06f, 0.07f, 0.10f, 0.97f);
    private static readonly Vector4 Popup = new(0.08f, 0.09f, 0.12f, 0.98f);
    private static readonly Vector4 Frame = new(0.13f, 0.14f, 0.18f, 1.00f);
    private static readonly Vector4 FrameHover = new(0.19f, 0.21f, 0.26f, 1.00f);
    private static readonly Vector4 FrameActive = new(0.10f, 0.18f, 0.32f, 1.00f);
    private static readonly Vector4 Control = new(0.17f, 0.19f, 0.24f, 1.00f);
    private static readonly Vector4 Title = new(0.04f, 0.06f, 0.11f, 1.00f);
    private static readonly Vector4 Tab = new(0.11f, 0.12f, 0.16f, 1.00f);
    private static readonly Vector4 Edge = new(1.00f, 1.00f, 1.00f, 0.08f);
    private static readonly Vector4 Line = new(1.00f, 1.00f, 1.00f, 0.10f);
    private static readonly Vector4 ScrollbarTrack = new(0.00f, 0.00f, 0.00f, 0.20f);
    private static readonly Vector4 ScrollbarGrab = new(0.24f, 0.26f, 0.32f, 1.00f);
    private static readonly Vector4 ScrollbarGrabHover = new(0.33f, 0.36f, 0.44f, 1.00f);
    private static readonly Vector4 Selection = new(0.10f, 0.42f, 0.86f, 0.35f);
    private static readonly Vector4 GripHover = new(0.10f, 0.42f, 0.86f, 0.60f);
    private static readonly Vector4 TableHeader = new(0.12f, 0.13f, 0.17f, 1.00f);
    private static readonly Vector4 TableRowAlt = new(1.00f, 1.00f, 1.00f, 0.03f);
    private static readonly Vector4 Clear = new(0.00f, 0.00f, 0.00f, 0.00f);

    public static void Apply(ImGuiStylePtr style)
    {
        Shape(style);
        Paint(style);
    }

    private static void Shape(ImGuiStylePtr style)
    {
        style.WindowRounding = WindowRounding;
        style.ChildRounding = WindowRounding;
        style.PopupRounding = PopupRounding;
        style.FrameRounding = FrameRounding;
        style.GrabRounding = FrameRounding;
        style.TabRounding = FrameRounding;
        style.ScrollbarRounding = WindowRounding;
        style.WindowPadding = WindowPadding;
        style.FramePadding = FramePadding;
        style.ItemSpacing = ItemSpacing;
        style.ItemInnerSpacing = ItemInnerSpacing;
        style.WindowBorderSize = WindowBorder;
        style.FrameBorderSize = NoBorder;
        style.TabBorderSize = NoBorder;
        style.WindowTitleAlign = TitleOnTheLeft;
        style.SeparatorTextBorderSize = SeparatorTextBorder;
    }

    private static void Paint(ImGuiStylePtr style)
    {
        var colors = style.Colors;
        colors[(int)ImGuiCol.Text] = Text;
        colors[(int)ImGuiCol.TextDisabled] = DisabledText;
        colors[(int)ImGuiCol.WindowBg] = Window;
        colors[(int)ImGuiCol.ChildBg] = Clear;
        colors[(int)ImGuiCol.PopupBg] = Popup;
        colors[(int)ImGuiCol.Border] = Edge;
        colors[(int)ImGuiCol.BorderShadow] = Clear;
        colors[(int)ImGuiCol.FrameBg] = Frame;
        colors[(int)ImGuiCol.FrameBgHovered] = FrameHover;
        colors[(int)ImGuiCol.FrameBgActive] = FrameActive;
        colors[(int)ImGuiCol.TitleBg] = Title;
        colors[(int)ImGuiCol.TitleBgActive] = AccentDark;
        colors[(int)ImGuiCol.TitleBgCollapsed] = Window;
        colors[(int)ImGuiCol.MenuBarBg] = Frame;
        colors[(int)ImGuiCol.ScrollbarBg] = ScrollbarTrack;
        colors[(int)ImGuiCol.ScrollbarGrab] = ScrollbarGrab;
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = ScrollbarGrabHover;
        colors[(int)ImGuiCol.ScrollbarGrabActive] = Accent;
        colors[(int)ImGuiCol.CheckMark] = AccentBright;
        colors[(int)ImGuiCol.SliderGrab] = Accent;
        colors[(int)ImGuiCol.SliderGrabActive] = AccentBright;
        colors[(int)ImGuiCol.Button] = Control;
        colors[(int)ImGuiCol.ButtonHovered] = AccentHover;
        colors[(int)ImGuiCol.ButtonActive] = Accent;
        colors[(int)ImGuiCol.Header] = Control;
        colors[(int)ImGuiCol.HeaderHovered] = AccentHover;
        colors[(int)ImGuiCol.HeaderActive] = Accent;
        colors[(int)ImGuiCol.Separator] = Line;
        colors[(int)ImGuiCol.SeparatorHovered] = AccentHover;
        colors[(int)ImGuiCol.SeparatorActive] = Accent;
        colors[(int)ImGuiCol.ResizeGrip] = Edge;
        colors[(int)ImGuiCol.ResizeGripHovered] = GripHover;
        colors[(int)ImGuiCol.ResizeGripActive] = Accent;
        colors[(int)ImGuiCol.Tab] = Tab;
        colors[(int)ImGuiCol.TabHovered] = AccentHover;
        colors[(int)ImGuiCol.TabSelected] = AccentDark;
        colors[(int)ImGuiCol.TabSelectedOverline] = Accent;
        colors[(int)ImGuiCol.TabDimmed] = Tab;
        colors[(int)ImGuiCol.TabDimmedSelected] = Control;
        colors[(int)ImGuiCol.TabDimmedSelectedOverline] = Line;
        colors[(int)ImGuiCol.PlotHistogram] = Accent;
        colors[(int)ImGuiCol.PlotHistogramHovered] = AccentBright;
        colors[(int)ImGuiCol.TableHeaderBg] = TableHeader;
        colors[(int)ImGuiCol.TableBorderStrong] = Line;
        colors[(int)ImGuiCol.TableBorderLight] = Edge;
        colors[(int)ImGuiCol.TableRowBg] = Clear;
        colors[(int)ImGuiCol.TableRowBgAlt] = TableRowAlt;
        colors[(int)ImGuiCol.TextLink] = AccentBright;
        colors[(int)ImGuiCol.TextSelectedBg] = Selection;
        colors[(int)ImGuiCol.NavCursor] = AccentBright;
    }
}
