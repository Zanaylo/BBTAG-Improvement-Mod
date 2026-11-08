using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Launcher.Ui;

internal sealed partial class LauncherWindow : Window
{
    private readonly LauncherController? _controller;

    public LauncherWindow()
    {
        InitializeComponent();
    }

    public LauncherWindow(LauncherController controller) : this()
    {
        _controller = controller;
        Show(controller.ViewModel);
        controller.ViewModelReplaced += Show;
        Closing += (_, _) => controller.Close();
    }

    private void Show(LauncherViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.Messages.CollectionChanged += OnMessagesChanged;
        MessageList.ScrollIntoView(viewModel.Messages.Count - 1);
    }

    private async void OnPlayClick(object? sender, RoutedEventArgs e)
    {
        if (_controller is null)
            return;

        await _controller.PlayAsync();
    }

    private async void OnRefreshModsClick(object? sender, RoutedEventArgs e)
    {
        if (_controller is not null)
            await _controller.RefreshModsAsync();
    }

    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        if (_controller is null)
            return;

        if (_controller.HasArchivesToUnpack)
            await _controller.RefreshModsAsync();

        await _controller.PlayOnStartAsync();
    }

    private void OnMoveUpClick(object? sender, RoutedEventArgs e) => MoveMod(sender, ModMove.Up);

    private void OnMoveDownClick(object? sender, RoutedEventArgs e) => MoveMod(sender, ModMove.Down);

    private static void MoveMod(object? sender, ModMove move)
    {
        if (sender is Control { DataContext: ModEntry mod })
            mod.Move(move);
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add)
            return;

        MessageList.UpdateLayout();
        MessageList.ScrollIntoView(e.NewStartingIndex);
    }
}
