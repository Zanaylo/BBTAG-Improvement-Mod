using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Menu;

internal static unsafe class MenuInput
{
    public const string ToggleKeyName = "F1";

    private const int ToggleKey = 0x70;

    private static readonly ConcurrentQueue<WindowInput> s_pending = new();
    private static readonly int[] s_presses = new int[Hotkeys.Count];

    private static nint s_previousProcedure;
    private static volatile bool s_isOpen;
    private static volatile bool s_wantsText;

    public static nint Window { get; private set; }

    public static bool IsOpen => s_isOpen;

    public static bool Install(nint window)
    {
        if (Window != 0 || window == 0)
            return Window != 0;

        s_previousProcedure = User32.SetWindowLong(
            window, User32.WindowProcedureIndex, (nint)(delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint>)&Procedure);

        if (s_previousProcedure == 0)
            return false;

        Window = window;
        return true;
    }

    public static void Close() => s_isOpen = false;

    public static void TypingInto(bool wantsText) => s_wantsText = wantsText;

    public static int TakePresses(Hotkey hotkey) => Interlocked.Exchange(ref s_presses[(int)hotkey], 0);

    public static bool IsHeld(Hotkey hotkey) =>
        !s_wantsText && Window != 0 && User32.GetForegroundWindow() == Window && User32.IsKeyDown(Hotkeys.KeyOf(hotkey));

    public static bool TryTake(out WindowInput? input) => s_pending.TryDequeue(out input);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint Procedure(nint window, uint message, nuint wParam, nint lParam)
    {
        if (Handle(WindowInput.Parse(message, wParam, lParam)))
            return 0;

        return User32.CallWindowProc(s_previousProcedure, window, message, wParam, lParam);
    }

    private static bool Handle(WindowInput? input)
    {
        switch (input)
        {
            case KeyPressed { Key: ToggleKey }:
                Toggle();
                return true;
            case KeyPressed pressed when !s_wantsText && Hotkeys.Of(pressed.Key) is { } hotkey:
                Interlocked.Increment(ref s_presses[(int)hotkey]);
                return true;
        }

        if (!s_isOpen || input is null)
            return false;

        if (input is KeyReleased)
        {
            s_pending.Enqueue(input);
            return s_wantsText;
        }

        if (input is KeyPressed or CharacterTyped && !s_wantsText)
            return false;

        s_pending.Enqueue(input);
        return true;
    }

    private static void Toggle()
    {
        s_pending.Clear();
        s_wantsText = false;
        s_isOpen = !s_isOpen;

        if (s_isOpen && CursorInWindow() is { } cursor)
            s_pending.Enqueue(cursor);
    }

    private static MouseMoved? CursorInWindow()
    {
        if (!User32.GetCursorPos(out var point) || !User32.ScreenToClient(Window, ref point))
            return null;

        return new MouseMoved(point.X, point.Y);
    }
}
