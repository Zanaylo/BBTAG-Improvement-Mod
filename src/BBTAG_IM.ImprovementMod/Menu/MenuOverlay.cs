using System.Diagnostics;
using System.Numerics;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.ImprovementMod.Graphics;
using BBTAG_IM.Native;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed unsafe class MenuOverlay(ImprovementMenu menu, MenuSettings settings, ILog log)
{
    public const string NativeLibraryName = "cimgui.dll";

    private const string FontFileName = "segoeui.ttf";
    private const float DefaultFontSize = 13f;
    private const double LongestFrameSeconds = 0.1;
    private const double ShortestFrameSeconds = 0.0001;

    private readonly ImGuiRenderer _renderer = new(log);

    private long _lastFrame = Stopwatch.GetTimestamp();
    private bool _contextCreated;
    private ImGuiStyle _unscaledStyle;
    private float _fontSize;
    private bool _wasOpen;

    public void OnPresent(nint device)
    {
        var isOpen = MenuInput.IsOpen;

        if (isOpen && !_wasOpen)
            menu.Reopened();

        _wasOpen = isOpen;

        if (isOpen)
            BackBufferPass.Run(device, Draw);
    }

    public void OnReset() => _renderer.Release();

    private void Draw(Direct3DDevice device, DeviceViewport viewport)
    {
        var displaySize = new Vector2(viewport.Width, viewport.Height);
        CreateContext();
        BuildFont(MenuFont.SizeFor(settings.Current.MenuFontSize, displaySize.Y));

        if (!_renderer.PrepareFonts(device))
            return;

        var io = ImGui.GetIO();
        io.DisplaySize = displaySize;
        io.DeltaTime = (float)Math.Clamp(Stopwatch.GetElapsedTime(_lastFrame).TotalSeconds, ShortestFrameSeconds, LongestFrameSeconds);
        _lastFrame = Stopwatch.GetTimestamp();
        FeedInput(io, displaySize);

        ImGui.NewFrame();

        if (!menu.Draw(displaySize))
            MenuInput.Close();

        ImGui.Render();
        MenuInput.TypingInto(io.WantTextInput);
        _renderer.Render(device, ImGui.GetDrawData());
    }

    private void CreateContext()
    {
        if (_contextCreated)
            return;

        _contextCreated = true;
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.NativePtr->IniFilename = null;
        io.MouseDrawCursor = true;

        ImGui.StyleColorsDark();
        _unscaledStyle = *ImGui.GetStyle().NativePtr;
    }

    private void BuildFont(float size)
    {
        if (size == _fontSize)
            return;

        _fontSize = size;
        _renderer.Release();

        var fonts = ImGui.GetIO().Fonts;
        var fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), FontFileName);
        fonts.Clear();

        if (File.Exists(fontPath))
            fonts.AddFontFromFileTTF(fontPath, size);
        else
            fonts.AddFontDefault();

        var style = ImGui.GetStyle();
        *style.NativePtr = _unscaledStyle;
        style.ScaleAllSizes(size / DefaultFontSize);
    }

    private static void FeedInput(ImGuiIOPtr io, Vector2 displaySize)
    {
        var clientSize = User32.GetClientRect(MenuInput.Window, out var client)
            ? new Vector2(client.Right - client.Left, client.Bottom - client.Top)
            : Vector2.Zero;

        while (MenuInput.TryTake(out var input))
        {
            switch (input)
            {
                case MouseMoved moved:
                    var point = WindowInput.ToDisplay(moved, clientSize, displaySize);
                    io.AddMousePosEvent(point.X, point.Y);
                    break;
                case MouseButton button:
                    io.AddMouseButtonEvent(button.Button, button.Down);
                    break;
                case MouseWheelTurned wheel:
                    io.AddMouseWheelEvent(0, wheel.Notches);
                    break;
                case KeyPressed pressed:
                    MenuKeys.Press(io, pressed.Key, true);
                    break;
                case KeyReleased released:
                    MenuKeys.Press(io, released.Key, false);
                    break;
                case CharacterTyped typed:
                    MenuKeys.Type(io, typed.Character);
                    break;
            }
        }
    }
}
