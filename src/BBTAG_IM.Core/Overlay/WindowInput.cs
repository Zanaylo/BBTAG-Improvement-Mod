using System.Numerics;

namespace BBTAG_IM.Core.Overlay;

public abstract record WindowInput
{
    private const uint KeyDown = 0x100;
    private const uint SystemKeyDown = 0x104;
    private const uint KeyUp = 0x101;
    private const uint SystemKeyUp = 0x105;
    private const uint Character = 0x102;
    private const uint MouseMove = 0x200;
    private const uint LeftButtonDown = 0x201;
    private const uint LeftButtonUp = 0x202;
    private const uint RightButtonDown = 0x204;
    private const uint RightButtonUp = 0x205;
    private const uint MiddleButtonDown = 0x207;
    private const uint MiddleButtonUp = 0x208;
    private const uint MouseWheel = 0x20A;
    private const long RepeatFlag = 1L << 30;
    private const float WheelNotch = 120f;
    private const int LeftButton = 0;
    private const int RightButton = 1;
    private const int MiddleButton = 2;

    public static WindowInput? Parse(uint message, ulong wParam, long lParam) => message switch
    {
        KeyDown or SystemKeyDown when (lParam & RepeatFlag) == 0 => new KeyPressed((int)wParam),
        KeyUp or SystemKeyUp => new KeyReleased((int)wParam),
        Character => new CharacterTyped((char)wParam),
        MouseMove => new MouseMoved((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF)),
        LeftButtonDown => new MouseButton(LeftButton, true),
        LeftButtonUp => new MouseButton(LeftButton, false),
        RightButtonDown => new MouseButton(RightButton, true),
        RightButtonUp => new MouseButton(RightButton, false),
        MiddleButtonDown => new MouseButton(MiddleButton, true),
        MiddleButtonUp => new MouseButton(MiddleButton, false),
        MouseWheel => new MouseWheelTurned((short)((wParam >> 16) & 0xFFFF) / WheelNotch),
        _ => null,
    };

    public static Vector2 ToDisplay(MouseMoved moved, Vector2 clientSize, Vector2 displaySize)
    {
        var point = new Vector2(moved.X, moved.Y);

        if (clientSize.X <= 0 || clientSize.Y <= 0)
            return point;

        return point * displaySize / clientSize;
    }
}

public sealed record KeyPressed(int Key) : WindowInput;

public sealed record KeyReleased(int Key) : WindowInput;

public sealed record CharacterTyped(char Character) : WindowInput;

public sealed record MouseMoved(int X, int Y) : WindowInput;

public sealed record MouseButton(int Button, bool Down) : WindowInput;

public sealed record MouseWheelTurned(float Notches) : WindowInput;
