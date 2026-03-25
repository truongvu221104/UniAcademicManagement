namespace UniAcademic.AdminApp.Dialogs;

public interface IMessageDialogService
{
    void ShowInfo(string message, string title = "UniAcademic");

    void ShowError(string message, string title = "Error");

    bool Confirm(string message, string title = "Confirm");
}
