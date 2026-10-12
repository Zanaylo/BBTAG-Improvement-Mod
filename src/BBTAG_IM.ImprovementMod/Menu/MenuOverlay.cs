using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.ImprovementMod.Graphics;
using BBTAG_IM.Native;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed unsafe class MenuOverlay
{
    public const string NativeLibraryName = "cimgui.dll";
    public const string LayoutFileName = "menu-layout.ini";

    private const string FontFileName = "segoeui.ttf";
    private const float DefaultFontSize = 13f;
    private const double LongestFrameSeconds = 0.1;
    private const double ShortestFrameSeconds = 0.0001;

    private readonly ImprovementMenu _menu;
    private readonly MenuSettings _settings;
    private readonly MenuTextures _textures;
    private readonly BackBufferPass _pass;
    private readonly ImGuiRenderer _renderer;
    private readonly string _layoutPath;
    private readonly Action<Direct3DDevice, DeviceViewport> _draw;

    private long _lastFrame = Stopwatch.GetTimestamp();
    private bool _contextCreated;
    private ImGuiStyle _unscaledStyle;
    private float _fontSize;
    private bool _wasOpen;

    public MenuOverlay(ImprovementMenu menu, MenuSettings settings, MenuTextures textures, BackBufferPass pass, string modFolder, ILog log)
    {
        _menu = menu;
        _settings = settings;
        _textures = textures;
        _pass = pass;
        _renderer = new ImGuiRenderer(textures, log);
        _layoutPath = Path.Combine(modFolder, LayoutFileName);
        _draw = Draw;
    }

    public void OnPresent(nint device)
    {
        var isOpen = MenuInput.IsOpen;

        if (isOpen != _wasOpen)
            Toggled(isOpen);

        _wasOpen = isOpen;

        if (isOpen)
            _pass.Run(device, _draw);
    }

    public void OnReset() => _textures.ReleaseAll();

    private void Toggled(bool isOpen)
    {
        if (isOpen)
        {
            _menu.Reopened();
            return;
        }

        if (_contextCreated)
            ImGui.SaveIniSettingsToDisk(_layoutPath);
    }

    private void Draw(Direct3DDevice device, DeviceViewport viewport)
    {
        var displaySize = new Vector2(viewport.Width, viewport.Height);
        CreateContext();
        BuildFont(MenuFont.SizeFor(_settings.Current.MenuFontSize, displaySize.Y));

        _textures.Use(device);

        if (!_renderer.PrepareFonts())
            return;

        var io = ImGui.GetIO();
        io.DisplaySize = displaySize;
        io.DeltaTime = (float)Math.Clamp(Stopwatch.GetElapsedTime(_lastFrame).TotalSeconds, ShortestFrameSeconds, LongestFrameSeconds);
        _lastFrame = Stopwatch.GetTimestamp();
        FeedInput(io, displaySize);

        ImGui.NewFrame();

        if (!_menu.Draw(displaySize))
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
        io.NativePtr->IniFilename = (byte*)Marshal.StringToCoTaskMemUTF8(_layoutPath);
        io.MouseDrawCursor = true;

        ImGui.StyleColorsDark();
        _unscaledStyle = *ImGui.GetStyle().NativePtr;
    }

    private void BuildFont(float size)
    {
        if (size == _fontSize)
            return;

        _fontSize = size;
        _renderer.ReleaseFont();

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
