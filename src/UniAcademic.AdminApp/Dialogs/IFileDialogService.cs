namespace UniAcademic.AdminApp.Dialogs;

public interface IFileDialogService
{
    string? SelectOpenFile(string filter = "All files (*.*)|*.*");

    string? SelectSaveFile(string suggestedFileName, string filter = "All files (*.*)|*.*");
}
