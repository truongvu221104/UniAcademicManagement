using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace UniAcademic.AdminApp.Dialogs;

public sealed class FormDialogService : IFormDialogService
{
    public bool Show(string title, IReadOnlyList<FormFieldViewModel> fields)
    {
        var workArea = SystemParameters.WorkArea;
        var maxDialogHeight = Math.Max(560, workArea.Height - 80);
        var initialDialogHeight = Math.Min(820, maxDialogHeight);

        var fieldStack = new StackPanel { Margin = new Thickness(24) };

        foreach (var field in fields)
        {
            var fieldCard = new Border
            {
                Margin = new Thickness(0, 0, 0, 14),
                Padding = new Thickness(14),
                Background = Brushes.White,
                BorderBrush = CreateBrush("#DCE5F0"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12)
            };

            var fieldPanel = new StackPanel();
            fieldPanel.Children.Add(new TextBlock
            {
                Text = field.Label,
                FontWeight = FontWeights.SemiBold,
                Foreground = CreateBrush("#18324A"),
                Margin = new Thickness(0, 0, 0, 4)
            });

            FrameworkElement editor;
            if (field.IsBoolean)
            {
                editor = new CheckBox
                {
                    Margin = new Thickness(0, 6, 0, 0),
                    Content = "Enabled"
                };
                editor.SetBinding(CheckBox.IsCheckedProperty, nameof(FormFieldViewModel.BoolValue));
            }
            else if (field.IsDate)
            {
                editor = new DatePicker
                {
                    Margin = new Thickness(0, 6, 0, 0),
                    MinWidth = 420,
                    SelectedDateFormat = DatePickerFormat.Short
                };
                editor.SetBinding(DatePicker.SelectedDateProperty, nameof(FormFieldViewModel.SelectedDate));
            }
            else if (field.IsEnum)
            {
                editor = new ComboBox
                {
                    ItemsSource = field.Options,
                    Margin = new Thickness(0, 6, 0, 0),
                    MinWidth = 420
                };
                editor.SetBinding(ComboBox.SelectedItemProperty, nameof(FormFieldViewModel.SelectedOption));
            }
            else if (field.IsLookup)
            {
                editor = BuildLookupEditor(field);
            }
            else
            {
                editor = new TextBox
                {
                    Margin = new Thickness(0, 6, 0, 0),
                    MinWidth = 420,
                    AcceptsReturn = field.IsMultiline,
                    TextWrapping = field.IsMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                    VerticalScrollBarVisibility = field.IsMultiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden,
                    Height = field.IsMultiline ? 100 : double.NaN
                };
                editor.SetBinding(TextBox.TextProperty, nameof(FormFieldViewModel.TextValue));
            }

            editor.DataContext = field;
            fieldPanel.Children.Add(editor);
            fieldCard.Child = fieldPanel;
            fieldStack.Children.Add(fieldCard);
        }

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

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var footer = new Border
        {
            Padding = new Thickness(24, 12, 24, 24),
            Background = CreateBrush("#F3F6FB"),
            Child = buttons
        };
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        var header = new Border
        {
            Margin = new Thickness(24, 24, 24, 0),
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
                        Text = "Review the fields below and save when the information is complete.",
                        Margin = new Thickness(0, 6, 0, 0),
                        Foreground = CreateBrush("#64748B"),
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var contentScrollViewer = new ScrollViewer
        {
            Margin = new Thickness(0, 12, 0, 0),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = fieldStack
        };
        Grid.SetRow(contentScrollViewer, 1);
        root.Children.Add(contentScrollViewer);

        var window = new Window
        {
            Title = title,
            Background = CreateBrush("#F3F6FB"),
            Content = root,
            Width = 680,
            Height = initialDialogHeight,
            MaxHeight = maxDialogHeight,
            MinHeight = 560,
            MinWidth = 620,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.CanResize,
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

    private static FrameworkElement BuildLookupEditor(FormFieldViewModel field)
    {
        var lookupPanel = new StackPanel
        {
            Margin = new Thickness(0, 6, 0, 0),
            DataContext = field
        };

        var lookupTextBox = new TextBox
        {
            MinWidth = 420
        };
        lookupTextBox.SetBinding(TextBox.TextProperty, new Binding(nameof(FormFieldViewModel.LookupText))
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay
        });

        var lookupListBox = new ListBox
        {
            ItemsSource = field.FilteredLookupOptions,
            DisplayMemberPath = nameof(FormOptionViewModel.Label),
            MinWidth = 420,
            MaxHeight = 160,
            Margin = new Thickness(0, 8, 0, 0),
            BorderBrush = CreateBrush("#D6DEE8"),
            BorderThickness = new Thickness(1)
        };

        void UpdateLookupVisibility()
        {
            lookupListBox.Visibility =
                lookupListBox.HasItems && (lookupTextBox.IsKeyboardFocusWithin || lookupListBox.IsKeyboardFocusWithin)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        lookupTextBox.TextChanged += (_, _) =>
        {
            if (lookupPanel.DataContext is FormFieldViewModel viewModel)
            {
                if (viewModel.SelectedLookupOption is not null &&
                    !string.Equals(viewModel.SelectedLookupOption.Label, lookupTextBox.Text, StringComparison.Ordinal))
                {
                    viewModel.SelectedLookupOption = null;
                }

                if (!string.Equals(viewModel.LookupText, lookupTextBox.Text, StringComparison.Ordinal))
                {
                    viewModel.LookupText = lookupTextBox.Text;
                }
            }

            UpdateLookupVisibility();
        };

        lookupTextBox.GotKeyboardFocus += (_, _) => UpdateLookupVisibility();
        lookupTextBox.LostKeyboardFocus += (_, _) =>
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(UpdateLookupVisibility));
        };

        void CommitLookupSelection(FormOptionViewModel selectedOption)
        {
            if (lookupPanel.DataContext is not FormFieldViewModel viewModel)
            {
                return;
            }

            viewModel.SelectedLookupOption = selectedOption;
            lookupTextBox.Text = selectedOption.Label;
            lookupTextBox.CaretIndex = lookupTextBox.Text.Length;
            lookupListBox.SelectedItem = null;
            lookupTextBox.Focus();
            UpdateLookupVisibility();
        }

        lookupListBox.SelectionChanged += (_, _) =>
        {
            if (lookupListBox.SelectedItem is FormOptionViewModel selectedOption)
            {
                CommitLookupSelection(selectedOption);
            }
        };

        lookupListBox.PreviewMouseLeftButtonUp += (_, args) =>
        {
            if ((args.OriginalSource as DependencyObject) is not null)
            {
                var item = ItemsControl.ContainerFromElement(lookupListBox, (DependencyObject)args.OriginalSource) as ListBoxItem;
                if (item?.DataContext is FormOptionViewModel selectedOption)
                {
                    CommitLookupSelection(selectedOption);
                    args.Handled = true;
                }
            }
        };

        lookupListBox.GotKeyboardFocus += (_, _) => UpdateLookupVisibility();
        lookupListBox.LostKeyboardFocus += (_, _) =>
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(UpdateLookupVisibility));
        };
        lookupListBox.PreviewKeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter && lookupListBox.SelectedItem is FormOptionViewModel selectedOption)
            {
                CommitLookupSelection(selectedOption);
                args.Handled = true;
            }
        };

        field.FilteredLookupOptions.CollectionChanged += (_, _) => UpdateLookupVisibility();

        lookupPanel.Children.Add(new TextBlock
        {
            Text = "Type to filter existing values, then choose the exact option below.",
            Foreground = CreateBrush("#64748B"),
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap
        });
        lookupPanel.Children.Add(lookupTextBox);
        lookupPanel.Children.Add(lookupListBox);
        return lookupPanel;
    }

    private static SolidColorBrush CreateBrush(string hex)
        => new((Color)ColorConverter.ConvertFromString(hex));
}
