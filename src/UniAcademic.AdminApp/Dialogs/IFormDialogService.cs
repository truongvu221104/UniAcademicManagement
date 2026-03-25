namespace UniAcademic.AdminApp.Dialogs;

public interface IFormDialogService
{
    bool Show(string title, IReadOnlyList<FormFieldViewModel> fields);
}
