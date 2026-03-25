using System.Windows;
using System.Windows.Controls;

namespace UniAcademic.AdminApp.Dialogs;

public sealed class TextEditorDialogService : ITextEditorDialogService
{
    public bool Edit(string title, ref string text)
    {
        var currentText = text;
        var editor = new TextBox
        {
            Text = currentText,
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            MinWidth = 780,
            MinHeight = 420,
            FontFamily = new System.Windows.Media.FontFamily("Consolas")
        };

        var okButton = new Button
        {
            Content = "Save",
            Width = 90,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 90,
            IsCancel = true
        };

        var root = new DockPanel { Margin = new Thickness(16) };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(editor);

        var window = new Window
        {
            Title = title,
            Content = root,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.MainWindow,
            Width = 860,
            Height = 560
        };

        okButton.Click += (_, _) =>
        {
            currentText = editor.Text;
            window.DialogResult = true;
            window.Close();
        };

        var result = window.ShowDialog() == true;
        if (result)
        {
            text = currentText;
        }

        return result;
    }
}
