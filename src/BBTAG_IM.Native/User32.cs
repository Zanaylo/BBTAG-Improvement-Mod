using System.Runtime.InteropServices;

namespace BBTAG_IM.Native;

[StructLayout(LayoutKind.Sequential)]
public struct WindowRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
public struct ScreenPoint
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct CursorInfo
{
    public int Size;
    public int Flags;
    public nint Cursor;
    public ScreenPoint Position;
}

public static partial class User32
{
    public const int WindowProcedureIndex = -4;

    private const int CursorShowing = 1;

    private const string Library = "user32.dll";
    private const short KeyDownBit = unchecked((short)0x8000);

    [LibraryImport(Library, EntryPoint = "SetWindowLongW", SetLastError = true)]
    public static partial nint SetWindowLong(nint window, int index, nint value);

    [LibraryImport(Library, EntryPoint = "CallWindowProcW")]
    public static partial nint CallWindowProc(nint previous, nint window, uint message, nuint wParam, nint lParam);

    [LibraryImport(Library)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetClientRect(nint window, out WindowRect rect);

    [LibraryImport(Library)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out ScreenPoint point);

    [LibraryImport(Library)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ScreenToClient(nint window, ref ScreenPoint point);

    [LibraryImport(Library)]
    public static partial nint GetForegroundWindow();

    [LibraryImport(Library)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint window);

    public static bool IsKeyDown(int key) => (GetAsyncKeyState(key) & KeyDownBit) != 0;

    public static unsafe bool IsCursorShowing()
    {
        var info = new CursorInfo { Size = sizeof(CursorInfo) };
        return GetCursorInfo(ref info) && (info.Flags & CursorShowing) != 0 && info.Cursor != 0;
    }

    [LibraryImport(Library)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorInfo(ref CursorInfo info);

    [LibraryImport(Library)]
    private static partial short GetAsyncKeyState(int key);
}
