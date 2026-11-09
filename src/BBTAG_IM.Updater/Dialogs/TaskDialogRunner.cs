using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Updater.Native;

namespace BBTAG_IM.Updater.Dialogs;

internal sealed unsafe class TaskDialogRunner : IDisposable
{
    public const string Title = "BBTAG Improvement Mod";

    private const int ProgressSteps = 1000;
    private const int NoButton = 0;

    private readonly List<nint> _allocations = [];
    private readonly nint _largeIcon;
    private readonly nint _smallIcon;
    private GCHandle _self;
    private TaskDialogPage? _page;
    private nint _window;

    public TaskDialogRunner()
    {
        nint large = 0;
        nint small = 0;

        if (Environment.ProcessPath is { } program)
            TaskDialogNative.ExtractIconEx(program, 0, &large, &small, 1);

        _largeIcon = large;
        _smallIcon = small;
        _self = GCHandle.Alloc(this);
    }

    public int Show(TaskDialogPage page)
    {
        _page = page;
        var config = Build(page);
        int button;
        var result = TaskDialogNative.TaskDialogIndirect(&config, &button, null, null);
        return result < 0 ? NoButton : button;
    }

    public void Navigate(TaskDialogPage page)
    {
        _page = page;
        var config = Build(page);
        TaskDialogNative.SendMessage(_window, TaskDialogNative.NavigatePage, 0, (nint)(&config));
    }

    public void SetProgress(double fraction)
    {
        var position = (int)(Math.Clamp(fraction, 0, 1) * ProgressSteps);
        TaskDialogNative.SendMessage(_window, TaskDialogNative.SetProgressBarPosition, position, 0);
    }

    public void SetContent(string text)
    {
        var pointer = Marshal.StringToHGlobalUni(text);

        try
        {
            TaskDialogNative.SendMessage(_window, TaskDialogNative.UpdateElementText, TaskDialogNative.ContentElement, pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    public void EnableButton(int id, bool enabled) =>
        TaskDialogNative.SendMessage(_window, TaskDialogNative.EnableButton, id, enabled ? 1 : 0);

    public void Dispose()
    {
        foreach (var allocation in _allocations)
            Marshal.FreeHGlobal(allocation);

        _allocations.Clear();

        if (_largeIcon != 0)
            TaskDialogNative.DestroyIcon(_largeIcon);

        if (_smallIcon != 0)
            TaskDialogNative.DestroyIcon(_smallIcon);

        if (_self.IsAllocated)
            _self.Free();
    }

    private TaskDialogConfig Build(TaskDialogPage page)
    {
        var buttons = (TaskDialogButtonEntry*)Allocate(Math.Max(1, page.Buttons.Count) * sizeof(TaskDialogButtonEntry));

        for (var index = 0; index < page.Buttons.Count; index++)
            buttons[index] = new TaskDialogButtonEntry { Id = page.Buttons[index].Id, Text = Text(page.Buttons[index].Text) };

        return new TaskDialogConfig
        {
            Size = (uint)sizeof(TaskDialogConfig),
            Flags = FlagsOf(page),
            CommonButtons = page.CommonButtons,
            WindowTitle = Text(Title),
            MainIcon = _largeIcon,
            MainInstruction = Text(page.Instruction),
            Content = Text(page.Content),
            ButtonCount = (uint)page.Buttons.Count,
            Buttons = buttons,
            Callback = (nint)(delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nint, int>)&OnNotification,
            CallbackData = GCHandle.ToIntPtr(_self),
        };
    }

    private int FlagsOf(TaskDialogPage page)
    {
        var flags = TaskDialogNative.EnableHyperlinks | TaskDialogNative.CallbackTimer | TaskDialogNative.SizeToContent;

        if (_largeIcon != 0)
            flags |= TaskDialogNative.UseMainIconHandle;

        if (page.CanCancel)
            flags |= TaskDialogNative.AllowDialogCancellation;

        if (page.Buttons.Count > 0)
            flags |= TaskDialogNative.UseCommandLinks;

        return flags | page.Progress switch
        {
            PageProgress.Bar => TaskDialogNative.ShowProgressBar,
            PageProgress.Marquee => TaskDialogNative.ShowMarqueeProgressBar,
            _ => 0,
        };
    }

    private nint Text(string text)
    {
        var pointer = Marshal.StringToHGlobalUni(text);
        _allocations.Add(pointer);
        return pointer;
    }

    private nint Allocate(int length)
    {
        var pointer = Marshal.AllocHGlobal(length);
        _allocations.Add(pointer);
        return pointer;
    }

    private int Handle(nint window, uint notification, nint wParam, nint lParam)
    {
        _window = window;
        var page = _page!;

        switch (notification)
        {
            case TaskDialogNative.Created:
                ShowIcons();
                Open(page);
                break;
            case TaskDialogNative.Navigated:
                Open(page);
                break;
            case TaskDialogNative.ButtonClicked:
                return page.ButtonClicked?.Invoke(this, (int)wParam) == true ? TaskDialogNative.KeepOpen : TaskDialogNative.Handled;
            case TaskDialogNative.HyperlinkClicked:
                page.LinkClicked?.Invoke(Marshal.PtrToStringUni(lParam) ?? "");
                break;
            case TaskDialogNative.Timer:
                page.Tick?.Invoke(this);
                break;
        }

        return TaskDialogNative.Handled;
    }

    private void Open(TaskDialogPage page)
    {
        if (page.Progress == PageProgress.Bar)
            TaskDialogNative.SendMessage(_window, TaskDialogNative.SetProgressBarRange, 0, ProgressSteps << 16);

        if (page.Progress == PageProgress.Marquee)
            TaskDialogNative.SendMessage(_window, TaskDialogNative.SetProgressBarMarquee, 1, 0);

        page.Opened?.Invoke(this);
    }

    private void ShowIcons()
    {
        if (_smallIcon != 0)
            TaskDialogNative.SendMessage(_window, TaskDialogNative.SetIcon, TaskDialogNative.SmallIcon, _smallIcon);

        if (_largeIcon != 0)
            TaskDialogNative.SendMessage(_window, TaskDialogNative.SetIcon, TaskDialogNative.BigIcon, _largeIcon);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int OnNotification(nint window, uint notification, nint wParam, nint lParam, nint data)
    {
        try
        {
            return ((TaskDialogRunner)GCHandle.FromIntPtr(data).Target!).Handle(window, notification, wParam, lParam);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return TaskDialogNative.Handled;
        }
    }
}
