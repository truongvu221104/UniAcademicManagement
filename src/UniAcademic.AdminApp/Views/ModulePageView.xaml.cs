using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UniAcademic.AdminApp.ViewModels;

namespace UniAcademic.AdminApp.Views;

public partial class ModulePageView : UserControl
{
    public ModulePageView()
    {
        InitializeComponent();
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ModulePageViewModel viewModel)
        {
            try
            {
                await viewModel.RefreshAsync();
            }
            catch (Exception ex)
            {
                viewModel.NotifyError(ex.Message);
            }
        }
    }

    private async void ApplyFiltersButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ModulePageViewModel viewModel)
        {
            try
            {
                await viewModel.RefreshAsync();
            }
            catch (Exception ex)
            {
                viewModel.NotifyError(ex.Message);
            }
        }
    }

    private async void ResetFiltersButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ModulePageViewModel viewModel)
        {
            try
            {
                viewModel.ResetFilters();
                await viewModel.RefreshAsync();
            }
            catch (Exception ex)
            {
                viewModel.NotifyError(ex.Message);
            }
        }
    }

    private void DataGrid_OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (ShouldHideColumn(sender as DataGrid, e.PropertyName))
        {
            e.Cancel = true;
            return;
        }

        var propertyType = Nullable.GetUnderlyingType(e.PropertyType) ?? e.PropertyType;
        if (propertyType == typeof(Guid)
            && (string.Equals(e.PropertyName, "Id", StringComparison.Ordinal)
                || e.PropertyName.EndsWith("Id", StringComparison.Ordinal)))
        {
            e.Cancel = true;
            return;
        }

        e.Column.Header = CreateFriendlyHeader(e.PropertyName);
        e.Column.MinWidth = GetColumnMinWidth(e.PropertyName);
        e.Column.Width = GetColumnWidth(e.PropertyName);

        if (ShouldCenterAlignColumn(e.PropertyName))
        {
            ApplyCenteredColumnStyle(e.Column);
        }
    }

    private static bool ShouldHideColumn(DataGrid? dataGrid, string propertyName)
    {
        var itemType = GetDataGridItemType(dataGrid);
        if (itemType is null)
        {
            return false;
        }

        if (string.Equals(propertyName, "EnrolledCount", StringComparison.Ordinal)
            && itemType.GetProperty("CapacityDisplay") is not null)
        {
            return true;
        }

        if (string.Equals(propertyName, "Capacity", StringComparison.Ordinal)
            && itemType.GetProperty("CapacityDisplay") is not null)
        {
            return true;
        }

        return false;
    }

    private static string CreateFriendlyHeader(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return propertyName;
        }

        if (string.Equals(propertyName, "CapacityDisplay", StringComparison.Ordinal))
        {
            return "Capacity";
        }

        var result = new System.Text.StringBuilder();
        result.Append(propertyName[0]);

        for (var index = 1; index < propertyName.Length; index++)
        {
            var current = propertyName[index];
            if (char.IsUpper(current) && !char.IsUpper(propertyName[index - 1]))
            {
                result.Append(' ');
            }

            result.Append(current);
        }

        return result.ToString();
    }

    private static DataGridLength GetColumnWidth(string propertyName)
    {
        if (string.Equals(propertyName, "CapacityDisplay", StringComparison.Ordinal))
        {
            return new DataGridLength(1.1, DataGridLengthUnitType.Star);
        }

        if (propertyName.Contains("Description", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Title", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("FullName", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "Name", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Name", StringComparison.OrdinalIgnoreCase))
        {
            return new DataGridLength(2.2, DataGridLengthUnitType.Star);
        }

        if (propertyName.EndsWith("Code", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "Code", StringComparison.OrdinalIgnoreCase))
        {
            return new DataGridLength(1.2, DataGridLengthUnitType.Star);
        }

        if (propertyName.Contains("Status", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Result", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Gender", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Type", StringComparison.OrdinalIgnoreCase))
        {
            return new DataGridLength(1.0, DataGridLengthUnitType.Star);
        }

        if (propertyName.Contains("Date", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Utc", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Year", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Term", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Credits", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Weight", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Score", StringComparison.OrdinalIgnoreCase))
        {
            return new DataGridLength(1.1, DataGridLengthUnitType.Star);
        }

        return new DataGridLength(1.4, DataGridLengthUnitType.Star);
    }

    private static double GetColumnMinWidth(string propertyName)
    {
        if (string.Equals(propertyName, "CapacityDisplay", StringComparison.Ordinal))
        {
            return 120;
        }

        if (propertyName.Contains("Description", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Title", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("FullName", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "Name", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Name", StringComparison.OrdinalIgnoreCase))
        {
            return 180;
        }

        if (propertyName.EndsWith("Code", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "Code", StringComparison.OrdinalIgnoreCase))
        {
            return 120;
        }

        if (propertyName.Contains("Status", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Result", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Gender", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Type", StringComparison.OrdinalIgnoreCase))
        {
            return 110;
        }

        return 130;
    }

    private static bool ShouldCenterAlignColumn(string propertyName)
        => string.Equals(propertyName, "CapacityDisplay", StringComparison.Ordinal)
           || propertyName.Contains("Status", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("DayOfWeek", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("StartPeriod", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("EndPeriod", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Credits", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Weight", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Score", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Year", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Term", StringComparison.OrdinalIgnoreCase)
           || propertyName.Contains("Count", StringComparison.OrdinalIgnoreCase);

    private static void ApplyCenteredColumnStyle(DataGridColumn column)
    {
        if (column is DataGridTextColumn textColumn)
        {
            var textStyle = new Style(typeof(TextBlock));
            textStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            textStyle.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            textColumn.ElementStyle = textStyle;
        }

        var headerStyle = new Style(typeof(DataGridColumnHeader));
        headerStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        column.HeaderStyle = headerStyle;
    }

    private static Type? GetDataGridItemType(DataGrid? dataGrid)
    {
        var item = dataGrid?.ItemsSource?.Cast<object?>().FirstOrDefault(x => x is not null);
        return item?.GetType();
    }
}
