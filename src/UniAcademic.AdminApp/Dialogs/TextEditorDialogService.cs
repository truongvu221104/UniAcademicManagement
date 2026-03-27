using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            Padding = new Thickness(12),
            FontFamily = new FontFamily("Consolas"),
            Background = Brushes.White,
            BorderBrush = CreateBrush("#D6DEE8"),
            BorderThickness = new Thickness(1)
        };

        var okButton = new Button
        {
            Content = "Save",
            Width = 110,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 110,
            Background = Brushes.White,
            Foreground = CreateBrush("#18324A"),
            BorderBrush = CreateBrush("#D6DEE8"),
            IsCancel = true
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);

        var root = new DockPanel();
        var footer = new Border
        {
            Padding = new Thickness(24, 0, 24, 24),
            Child = buttons
        };
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);

        var header = new Border
        {
            Margin = new Thickness(24, 24, 24, 16),
            Padding = new Thickness(18),
            Background = CreateBrush("#F5F8FC"),
            BorderBrush = CreateBrush("#DCE5F0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 22,
                        FontWeight = FontWeights.Bold,
                        Foreground = CreateBrush("#18324A")
                    },
                    new TextBlock
                    {
                        Text = "Review the structured text below, then save the updated content.",
                        Margin = new Thickness(0, 6, 0, 0),
                        Foreground = CreateBrush("#64748B"),
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        root.Children.Add(new Border
        {
            Margin = new Thickness(24, 0, 24, 0),
            Background = Brushes.White,
            BorderBrush = CreateBrush("#DCE5F0"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Child = editor
        });

        var window = new Window
        {
            Title = title,
            Background = CreateBrush("#F3F6FB"),
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

    private static SolidColorBrush CreateBrush(string hex)
        => new((Color)ColorConverter.ConvertFromString(hex));
}
