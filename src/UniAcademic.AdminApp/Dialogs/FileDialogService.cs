using Microsoft.Win32;

namespace UniAcademic.AdminApp.Dialogs;

public sealed class FileDialogService : IFileDialogService
{
    public string? SelectOpenFile(string filter = "All files (*.*)|*.*")
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectSaveFile(string suggestedFileName, string filter = "All files (*.*)|*.*")
    {
        var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = filter,
            AddExtension = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
