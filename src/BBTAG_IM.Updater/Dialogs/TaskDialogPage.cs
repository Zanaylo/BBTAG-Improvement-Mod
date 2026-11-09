namespace BBTAG_IM.Updater.Dialogs;

internal enum PageProgress
{
    None,
    Bar,
    Marquee,
}

internal sealed record DialogButton(int Id, string Text);

internal sealed class TaskDialogPage
{
    public required string Instruction { get; init; }

    public string Content { get; init; } = "";

    public IReadOnlyList<DialogButton> Buttons { get; init; } = [];

    public int CommonButtons { get; init; }

    public PageProgress Progress { get; init; }

    public bool CanCancel { get; init; } = true;

    public Action<TaskDialogRunner>? Opened { get; init; }

    public Action<TaskDialogRunner>? Tick { get; init; }

    public Func<TaskDialogRunner, int, bool>? ButtonClicked { get; init; }

    public Action<string>? LinkClicked { get; init; }
}
