using System.Windows;
using System.Windows.Controls;

namespace UniAcademic.AdminApp.Dialogs;

public sealed class FormDialogService : IFormDialogService
{
    public bool Show(string title, IReadOnlyList<FormFieldViewModel> fields)
    {
        var panel = new StackPanel { Margin = new Thickness(16) };

        foreach (var field in fields)
        {
            panel.Children.Add(new TextBlock
            {
                Text = field.Label,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            FrameworkElement editor;
            if (field.IsBoolean)
            {
                editor = new CheckBox
                {
                    Margin = new Thickness(0, 0, 0, 12)
                };
                editor.SetBinding(CheckBox.IsCheckedProperty, nameof(FormFieldViewModel.BoolValue));
            }
            else if (field.IsEnum)
            {
                editor = new ComboBox
                {
                    ItemsSource = field.Options,
                    Margin = new Thickness(0, 0, 0, 12),
                    MinWidth = 320
                };
                editor.SetBinding(ComboBox.SelectedItemProperty, nameof(FormFieldViewModel.SelectedOption));
            }
            else
            {
                editor = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 12),
                    MinWidth = 320,
                    AcceptsReturn = field.IsMultiline,
                    TextWrapping = field.IsMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                    VerticalScrollBarVisibility = field.IsMultiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden,
                    Height = field.IsMultiline ? 100 : double.NaN
                };
                editor.SetBinding(TextBox.TextProperty, nameof(FormFieldViewModel.TextValue));
            }

            editor.DataContext = field;
            panel.Children.Add(editor);
        }

        var okButton = new Button
        {
            Content = "OK",
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

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);
        panel.Children.Add(buttons);

        var window = new Window
        {
            Title = title,
            Content = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = panel
            },
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Owner = Application.Current.MainWindow
        };

        okButton.Click += (_, _) =>
        {
            foreach (var field in fields)
            {
                if (!field.TryGetValue(out _, out var error))
                {
                    MessageBox.Show(error, "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            window.DialogResult = true;
            window.Close();
        };

        return window.ShowDialog() == true;
    }
}
