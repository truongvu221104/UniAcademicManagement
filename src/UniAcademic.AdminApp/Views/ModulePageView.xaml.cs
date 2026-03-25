using System.Windows;
using System.Windows.Controls;
using UniAcademic.AdminApp.ViewModels;

namespace UniAcademic.AdminApp.Views;

public partial class ModulePageView : UserControl
{
    public ModulePageView()
    {
        InitializeComponent();
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ModulePageViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }
}
