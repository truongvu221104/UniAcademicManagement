using System.Windows;
using UniAcademic.AdminApp.ViewModels;

namespace UniAcademic.AdminApp;

public partial class ShellWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public ShellWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += ShellWindow_OnLoaded;
    }

    private async void ShellWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }
}
