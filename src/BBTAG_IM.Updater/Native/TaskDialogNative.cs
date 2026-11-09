using System.Runtime.InteropServices;

namespace BBTAG_IM.Updater.Native;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TaskDialogConfig
{
    public uint Size;
    public nint Parent;
    public nint Instance;
    public int Flags;
    public int CommonButtons;
    public nint WindowTitle;
    public nint MainIcon;
    public nint MainInstruction;
    public nint Content;
    public uint ButtonCount;
    public TaskDialogButtonEntry* Buttons;
    public int DefaultButton;
    public uint RadioButtonCount;
    public nint RadioButtons;
    public int DefaultRadioButton;
    public nint VerificationText;
    public nint ExpandedInformation;
    public nint ExpandedControlText;
    public nint CollapsedControlText;
    public nint FooterIcon;
    public nint Footer;
    public nint Callback;
    public nint CallbackData;
    public uint Width;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TaskDialogButtonEntry
{
    public int Id;
    public nint Text;
}

internal static unsafe partial class TaskDialogNative
{
    public const int EnableHyperlinks = 0x0001;
    public const int UseMainIconHandle = 0x0002;
    public const int AllowDialogCancellation = 0x0008;
    public const int UseCommandLinks = 0x0010;
    public const int ShowProgressBar = 0x0200;
    public const int ShowMarqueeProgressBar = 0x0400;
    public const int CallbackTimer = 0x0800;
    public const int SizeToContent = 0x01000000;

    public const int CancelButton = 0x0008;
    public const int CloseButton = 0x0020;

    public const int CancelId = 2;
    public const int CloseId = 8;

    public const uint Created = 0;
    public const uint Navigated = 1;
    public const uint ButtonClicked = 2;
    public const uint HyperlinkClicked = 3;
    public const uint Timer = 4;
    public const uint Destroyed = 5;

    public const int Handled = 0;
    public const int KeepOpen = 1;

    private const uint UserMessage = 0x0400;
    public const uint NavigatePage = UserMessage + 101;
    public const uint ClickButton = UserMessage + 102;
    public const uint SetProgressBarMarquee = UserMessage + 107;
    public const uint SetProgressBarRange = UserMessage + 105;
    public const uint SetProgressBarPosition = UserMessage + 106;
    public const uint EnableButton = UserMessage + 111;
    public const uint UpdateElementText = UserMessage + 114;
    public const int ContentElement = 0;

    public const uint SetIcon = 0x0080;
    public const int SmallIcon = 0;
    public const int BigIcon = 1;

    [LibraryImport("comctl32.dll")]
    public static partial int TaskDialogIndirect(TaskDialogConfig* config, int* button, int* radioButton, int* verificationChecked);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    public static partial nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    [LibraryImport("shell32.dll", EntryPoint = "ExtractIconExW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint ExtractIconEx(string file, int index, nint* large, nint* small, uint count);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyIcon(nint icon);
}
