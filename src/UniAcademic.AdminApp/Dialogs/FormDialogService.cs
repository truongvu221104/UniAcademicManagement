using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
            else if (field.IsLookup)
            {
                var lookupPanel = new StackPanel
                {
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var lookupTextBox = new TextBox
                {
                    MinWidth = 320
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
                    MinWidth = 320,
                    MaxHeight = 160,
                    Margin = new Thickness(0, 6, 0, 0)
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

                lookupTextBox.GotKeyboardFocus += (_, _) =>
                {
                    UpdateLookupVisibility();
                };

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

                lookupPanel.Children.Add(lookupTextBox);
                lookupPanel.Children.Add(lookupListBox);
                editor = lookupPanel;
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
