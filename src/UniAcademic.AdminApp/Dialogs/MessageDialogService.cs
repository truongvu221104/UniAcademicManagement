using System.Windows;

namespace UniAcademic.AdminApp.Dialogs;

public sealed class MessageDialogService : IMessageDialogService
{
    public bool Confirm(string message, string title = "Confirm")
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void ShowError(string message, string title = "Error")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string message, string title = "UniAcademic")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
