using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;

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

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            MessageList.ScrollIntoView(e.NewStartingIndex);
    }
}
