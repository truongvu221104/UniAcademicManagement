using System.Collections.ObjectModel;
using UniAcademic.AdminApp.Infrastructure;

namespace UniAcademic.AdminApp.ViewModels;

public sealed class ModulePageViewModel : ObservableObject
{
    private readonly Func<CancellationToken, Task<IReadOnlyCollection<object>>> _loadListAsync;
    private readonly Func<object, CancellationToken, Task<object?>>? _loadDetailAsync;
    private Func<CancellationToken, Task>? _beforeRefreshAsync;
    private object? _selectedItem;
    private string _detailsText = string.Empty;
    private string _searchText = string.Empty;
    private string _statusMessage = string.Empty;
    private ModuleNotificationKind _statusKind = ModuleNotificationKind.Info;
    private bool _isBusy;

    public ModulePageViewModel(
        string title,
        Func<CancellationToken, Task<IReadOnlyCollection<object>>> loadListAsync,
        Func<object, CancellationToken, Task<object?>>? loadDetailAsync = null)
    {
        Title = title;
        _loadListAsync = loadListAsync;
        _loadDetailAsync = loadDetailAsync;
    }

    public string Title { get; }

    public ObservableCollection<object> Items { get; } = [];

    public ObservableCollection<object> FilteredItems { get; } = [];

    public ObservableCollection<ModuleActionViewModel> Actions { get; } = [];

    public ObservableCollection<ModuleFilterFieldViewModel> Filters { get; } = [];

    public ObservableCollection<ModuleDetailLineViewModel> DetailLines { get; } = [];

    public ObservableCollection<ModuleStatusChipViewModel> StatusChips { get; } = [];

    public int ItemCount => Items.Count;

    public int FilteredCount => FilteredItems.Count;

    public bool HasFilters => Filters.Count > 0;

    public bool HasSelection => SelectedItem is not null;

    public string SelectedItemTitle => BuildSelectedItemTitle(SelectedItem);

    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                RaisePropertyChanged(nameof(HasSelection));
                RaisePropertyChanged(nameof(SelectedItemTitle));
                _ = LoadDetailsAsync();
                RaiseActionStates();
            }
        }
    }

    public string DetailsText
    {
        get => _detailsText;
        set => SetProperty(ref _detailsText, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                RaisePropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public ModuleNotificationKind StatusKind
    {
        get => _statusKind;
        set => SetProperty(ref _statusKind, value);
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseActionStates();
            }
        }
    }

    public void AddAction(ModuleActionViewModel action) => Actions.Add(action);

    public void SetBeforeRefreshAsync(Func<CancellationToken, Task> beforeRefreshAsync)
        => _beforeRefreshAsync = beforeRefreshAsync;

    public void AddFilter(ModuleFilterFieldViewModel filter)
    {
        Filters.Add(filter);
        RaisePropertyChanged(nameof(HasFilters));
    }

    public void ResetFilters()
    {
        foreach (var filter in Filters)
        {
            filter.Clear();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            NotifyInfo("Loading data...");
            if (_beforeRefreshAsync is not null)
            {
                await _beforeRefreshAsync(cancellationToken);
            }
            var items = await _loadListAsync(cancellationToken);
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            RaisePropertyChanged(nameof(ItemCount));
            ApplyFilter();
            NotifyInfo($"Loaded {Items.Count} item(s).");
            if (SelectedItem is null && Items.Count > 0)
            {
                SelectedItem = FilteredItems.FirstOrDefault() ?? Items[0];
            }
            else
            {
                await LoadDetailsAsync(cancellationToken);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void NotifyInfo(string message)
    {
        StatusKind = ModuleNotificationKind.Info;
        StatusMessage = message;
    }

    public void NotifySuccess(string message)
    {
        StatusKind = ModuleNotificationKind.Success;
        StatusMessage = message;
    }

    public void NotifyError(string message)
    {
        StatusKind = ModuleNotificationKind.Error;
        StatusMessage = message;
    }

    public void RaiseActionStates()
    {
        foreach (var action in Actions)
        {
            action.RaiseCanExecuteChanged();
        }
    }

    private async Task LoadDetailsAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedItem is null)
        {
            DetailsText = string.Empty;
            DetailLines.Clear();
            StatusChips.Clear();
            return;
        }

        var detail = _loadDetailAsync is null
            ? SelectedItem
            : await _loadDetailAsync(SelectedItem, cancellationToken) ?? SelectedItem;

        DetailsText = JsonFormatter.Format(detail);
        PopulateDetailLines(detail);
    }

    private void ApplyFilter()
    {
        var selectedId = _selectedItem is null ? null : TryGetEntityId(_selectedItem);
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? Items.ToList()
            : Items.Where(MatchesSearch).ToList();

        FilteredItems.Clear();
        foreach (var item in filtered)
        {
            FilteredItems.Add(item);
        }

        RaisePropertyChanged(nameof(FilteredCount));

        if (selectedId is not null)
        {
            SelectedItem = FilteredItems.FirstOrDefault(x => TryGetEntityId(x) == selectedId);
        }
        else if (FilteredItems.Count == 0)
        {
            SelectedItem = null;
        }
    }

    private bool MatchesSearch(object item)
    {
        var query = SearchText.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return item.GetType()
            .GetProperties()
            .Where(x => x.CanRead)
            .Select(x => x.GetValue(item)?.ToString())
            .Any(x => !string.IsNullOrWhiteSpace(x) && x.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static Guid? TryGetEntityId(object item)
    {
        var property = item.GetType().GetProperty("Id");
        if (property?.PropertyType != typeof(Guid))
        {
            return null;
        }

        return (Guid)property.GetValue(item)!;
    }

    private void PopulateDetailLines(object detail)
    {
        DetailLines.Clear();
        StatusChips.Clear();

        foreach (var property in detail.GetType().GetProperties().Where(x => x.CanRead))
        {
            if (ShouldHideCompanionProperty(detail, property.Name))
            {
                continue;
            }

            var value = property.GetValue(detail);
            if (TryCreateChip(property.Name, value, out var chip))
            {
                StatusChips.Add(chip);
            }

            if (!ShouldDisplayProperty(property.Name, property.PropertyType))
            {
                continue;
            }

            DetailLines.Add(new ModuleDetailLineViewModel(
                CreateFriendlyLabel(property.Name),
                FormatValue(value)));
        }
    }

    private static bool ShouldHideCompanionProperty(object detail, string propertyName)
    {
        var detailType = detail.GetType();
        if (detailType.GetProperty("CapacityDisplay") is null)
        {
            return false;
        }

        return string.Equals(propertyName, "Capacity", StringComparison.Ordinal)
               || string.Equals(propertyName, "EnrolledCount", StringComparison.Ordinal);
    }

    private static bool ShouldDisplayProperty(string propertyName, Type propertyType)
    {
        var actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (IsTechnicalIdProperty(propertyName, actualType))
        {
            return false;
        }

        if (actualType == typeof(string)
            || actualType == typeof(Guid)
            || actualType == typeof(DateTime)
            || actualType == typeof(DateTimeOffset)
            || actualType == typeof(bool)
            || actualType == typeof(decimal)
            || actualType == typeof(double)
            || actualType == typeof(float)
            || actualType == typeof(int)
            || actualType == typeof(long)
            || actualType == typeof(short)
            || actualType == typeof(byte)
            || actualType.IsEnum)
        {
            return true;
        }

        return false;
    }

    private static bool IsTechnicalIdProperty(string propertyName, Type propertyType)
    {
        if (propertyType != typeof(Guid))
        {
            return false;
        }

        return string.Equals(propertyName, "Id", StringComparison.Ordinal)
               || propertyName.EndsWith("Id", StringComparison.Ordinal);
    }

    private static string CreateFriendlyLabel(string propertyName)
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

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "—";
        }

        return value switch
        {
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss zzz"),
            bool boolean => boolean ? "Yes" : "No",
            _ => value.ToString() ?? "—"
        };
    }

    private static bool TryCreateChip(string propertyName, object? value, out ModuleStatusChipViewModel chip)
    {
        chip = null!;

        if (value is null)
        {
            return false;
        }

        if (string.Equals(propertyName, "Status", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "Result", StringComparison.OrdinalIgnoreCase))
        {
            var statusText = value.ToString();
            if (string.IsNullOrWhiteSpace(statusText))
            {
                return false;
            }

            chip = new ModuleStatusChipViewModel(statusText, InferChipKind(statusText));
            return true;
        }

        if (value is bool boolean && IsBooleanStatusProperty(propertyName))
        {
            chip = new ModuleStatusChipViewModel(
                FormatBooleanChipLabel(propertyName, boolean),
                InferChipKind(propertyName, boolean));
            return true;
        }

        return false;
    }

    private static bool IsBooleanStatusProperty(string propertyName)
        => propertyName.StartsWith("Is", StringComparison.Ordinal)
           || propertyName.StartsWith("Has", StringComparison.Ordinal);

    private static string FormatBooleanChipLabel(string propertyName, bool value)
    {
        var normalized = propertyName.StartsWith("Is", StringComparison.Ordinal)
            ? propertyName[2..]
            : propertyName.StartsWith("Has", StringComparison.Ordinal)
                ? propertyName[3..]
                : propertyName;

        return value ? normalized : $"Not {normalized}";
    }

    private static ModuleStatusChipKind InferChipKind(string text)
    {
        if (text.Contains("active", StringComparison.OrdinalIgnoreCase)
            || text.Contains("success", StringComparison.OrdinalIgnoreCase)
            || text.Contains("pass", StringComparison.OrdinalIgnoreCase)
            || text.Contains("publish", StringComparison.OrdinalIgnoreCase)
            || text.Contains("finalized", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleStatusChipKind.Success;
        }

        if (text.Contains("warning", StringComparison.OrdinalIgnoreCase)
            || text.Contains("pending", StringComparison.OrdinalIgnoreCase)
            || text.Contains("draft", StringComparison.OrdinalIgnoreCase)
            || text.Contains("reopen", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleStatusChipKind.Warning;
        }

        if (text.Contains("inactive", StringComparison.OrdinalIgnoreCase)
            || text.Contains("fail", StringComparison.OrdinalIgnoreCase)
            || text.Contains("error", StringComparison.OrdinalIgnoreCase)
            || text.Contains("absent", StringComparison.OrdinalIgnoreCase)
            || text.Contains("unpublish", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleStatusChipKind.Danger;
        }

        return ModuleStatusChipKind.Neutral;
    }

    private static ModuleStatusChipKind InferChipKind(string propertyName, bool value)
    {
        if (propertyName.Contains("Published", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Passed", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Finalized", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Primary", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Active", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Success", StringComparison.OrdinalIgnoreCase))
        {
            return value ? ModuleStatusChipKind.Success : ModuleStatusChipKind.Neutral;
        }

        return value ? ModuleStatusChipKind.Warning : ModuleStatusChipKind.Neutral;
    }

    private static string BuildSelectedItemTitle(object? item)
    {
        if (item is null)
        {
            return "No item selected";
        }

        var code = item.GetType().GetProperty("Code")?.GetValue(item)?.ToString();
        var name = item.GetType().GetProperty("Name")?.GetValue(item)?.ToString()
            ?? item.GetType().GetProperty("Title")?.GetValue(item)?.ToString()
            ?? item.GetType().GetProperty("FullName")?.GetValue(item)?.ToString();

        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
        {
            return $"{code} - {name}";
        }

        return code
            ?? name
            ?? item.GetType().Name;
    }
}

public sealed class ModuleDetailLineViewModel
{
    public ModuleDetailLineViewModel(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    public string Value { get; }
}

public sealed class ModuleFilterFieldViewModel : ObservableObject
{
    private string _textValue = string.Empty;
    private object? _selectedValue;

    private ModuleFilterFieldViewModel(string key, string label, ModuleFilterFieldType type, string? placeholder = null)
    {
        Key = key;
        Label = label;
        Type = type;
        Placeholder = placeholder ?? string.Empty;
    }

    public string Key { get; }

    public string Label { get; }

    public ModuleFilterFieldType Type { get; }

    public string Placeholder { get; }

    public ObservableCollection<ModuleFilterOptionViewModel> Options { get; } = [];

    public string TextValue
    {
        get => _textValue;
        set => SetProperty(ref _textValue, value);
    }

    public object? SelectedValue
    {
        get => _selectedValue;
        set => SetProperty(ref _selectedValue, value);
    }

    public Guid? SelectedGuidValue => SelectedValue is Guid guid ? guid : null;

    public static ModuleFilterFieldViewModel CreateText(string key, string label, string? placeholder = null)
        => new(key, label, ModuleFilterFieldType.Text, placeholder);

    public static ModuleFilterFieldViewModel CreateLookup(string key, string label, IEnumerable<ModuleFilterOptionViewModel> options, string? placeholder = null)
    {
        var field = new ModuleFilterFieldViewModel(key, label, ModuleFilterFieldType.Lookup, placeholder);
        foreach (var option in options)
        {
            field.Options.Add(option);
        }

        return field;
    }

    public void Clear()
    {
        TextValue = string.Empty;
        SelectedValue = null;
    }
}

public sealed class ModuleFilterOptionViewModel
{
    public ModuleFilterOptionViewModel(string label, object? value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    public object? Value { get; }
}

public enum ModuleFilterFieldType
{
    Text = 0,
    Lookup = 1
}

public sealed class ModuleStatusChipViewModel
{
    public ModuleStatusChipViewModel(string label, ModuleStatusChipKind kind)
    {
        Label = label;
        Kind = kind;
    }

    public string Label { get; }

    public ModuleStatusChipKind Kind { get; }
}

public enum ModuleStatusChipKind
{
    Neutral = 0,
    Success = 1,
    Warning = 2,
    Danger = 3
}

public enum ModuleNotificationKind
{
    Info = 0,
    Success = 1,
    Error = 2
}
