using System.Collections.ObjectModel;
using UniAcademic.AdminApp.Infrastructure;

namespace UniAcademic.AdminApp.Dialogs;

public sealed class FormFieldViewModel : ObservableObject
{
    private string _textValue = string.Empty;
    private bool _boolValue;
    private DateTime? _selectedDate;
    private string? _selectedOption;
    private FormOptionViewModel? _selectedLookupOption;
    private string _lookupText = string.Empty;

    public FormFieldViewModel(string label, Type valueType, object? initialValue = null, bool isMultiline = false, IEnumerable<FormOptionViewModel>? lookupOptions = null)
    {
        Label = label;
        ValueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        IsNullable = Nullable.GetUnderlyingType(valueType) is not null || !valueType.IsValueType;
        IsMultiline = isMultiline;

        if (ValueType == typeof(bool))
        {
            _boolValue = initialValue as bool? ?? false;
        }
        else if (ValueType == typeof(DateTime))
        {
            _selectedDate = initialValue switch
            {
                DateTime dateTime => dateTime,
                DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
                _ => null
            };
        }
        else if (ValueType.IsEnum)
        {
            foreach (var name in Enum.GetNames(ValueType))
            {
                Options.Add(name);
            }

            _selectedOption = initialValue?.ToString() ?? Options.FirstOrDefault();
        }
        else if (lookupOptions is not null)
        {
            foreach (var option in lookupOptions)
            {
                LookupOptions.Add(option);
            }

            ApplyLookupFilter();
            _selectedLookupOption = LookupOptions.FirstOrDefault(x => Equals(x.Value, initialValue));
            _lookupText = _selectedLookupOption?.Label ?? string.Empty;
            ApplyLookupFilter();
        }
        else
        {
            _textValue = initialValue switch
            {
                DateTime dateTime => dateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                _ => initialValue?.ToString() ?? string.Empty
            };
        }
    }

    public string Label { get; }

    public Type ValueType { get; }

    public bool IsNullable { get; }

    public bool IsMultiline { get; }

    public bool IsBoolean => ValueType == typeof(bool);

    public bool IsDate => ValueType == typeof(DateTime);

    public bool IsEnum => ValueType.IsEnum;

    public bool IsLookup => LookupOptions.Count > 0;

    public ObservableCollection<string> Options { get; } = [];

    public ObservableCollection<FormOptionViewModel> LookupOptions { get; } = [];

    public ObservableCollection<FormOptionViewModel> FilteredLookupOptions { get; } = [];

    public string TextValue
    {
        get => _textValue;
        set => SetProperty(ref _textValue, value);
    }

    public bool BoolValue
    {
        get => _boolValue;
        set => SetProperty(ref _boolValue, value);
    }

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set => SetProperty(ref _selectedDate, value);
    }

    public string? SelectedOption
    {
        get => _selectedOption;
        set => SetProperty(ref _selectedOption, value);
    }

    public FormOptionViewModel? SelectedLookupOption
    {
        get => _selectedLookupOption;
        set
        {
            if (SetProperty(ref _selectedLookupOption, value))
            {
                if (value is not null && !string.Equals(LookupText, value.Label, StringComparison.Ordinal))
                {
                    LookupText = value.Label;
                }
            }
        }
    }

    public string LookupText
    {
        get => _lookupText;
        set
        {
            if (SetProperty(ref _lookupText, value))
            {
                ApplyLookupFilter();
            }
        }
    }

    public bool TryGetValue(out object? value, out string? error)
    {
        error = null;
        value = null;

        if (IsBoolean)
        {
            value = BoolValue;
            return true;
        }

        if (IsEnum)
        {
            if (string.IsNullOrWhiteSpace(SelectedOption))
            {
                error = $"{Label} is required.";
                return false;
            }

            value = Enum.Parse(ValueType, SelectedOption, true);
            return true;
        }

        if (IsDate)
        {
            if (SelectedDate.HasValue)
            {
                value = SelectedDate.Value;
                return true;
            }

            if (IsNullable)
            {
                value = null;
                return true;
            }

            error = $"{Label} is required.";
            return false;
        }

        if (IsLookup)
        {
            if (SelectedLookupOption is not null)
            {
                value = SelectedLookupOption.Value;
                return true;
            }

            if (string.IsNullOrWhiteSpace(LookupText))
            {
                if (IsNullable)
                {
                    value = null;
                    return true;
                }

                error = $"{Label} is required.";
                return false;
            }

            var matchedOption = LookupOptions.FirstOrDefault(x =>
                string.Equals(x.Label, LookupText, StringComparison.OrdinalIgnoreCase))
                ?? LookupOptions.FirstOrDefault(x =>
                    x.Label.StartsWith(LookupText, StringComparison.OrdinalIgnoreCase))
                ?? LookupOptions.FirstOrDefault(x =>
                    x.Label.Contains(LookupText, StringComparison.OrdinalIgnoreCase));

            if (matchedOption is null)
            {
                error = $"{Label} must be selected from the available options.";
                return false;
            }

            SelectedLookupOption = matchedOption;
            value = matchedOption.Value;
            return true;
        }

        if (string.IsNullOrWhiteSpace(TextValue))
        {
            if (IsNullable)
            {
                value = null;
                return true;
            }

            error = $"{Label} is required.";
            return false;
        }

        try
        {
            value = ValueType == typeof(string) ? TextValue :
                ValueType == typeof(Guid) ? Guid.Parse(TextValue) :
                ValueType == typeof(int) ? int.Parse(TextValue) :
                ValueType == typeof(decimal) ? decimal.Parse(TextValue) :
                ValueType == typeof(DateTime) ? DateTime.Parse(TextValue) :
                ValueType == typeof(long) ? long.Parse(TextValue) :
                Convert.ChangeType(TextValue, ValueType);

            return true;
        }
        catch
        {
            error = $"{Label} has an invalid value.";
            return false;
        }
    }

    public static FormFieldViewModel CreateLookup(
        string label,
        Type valueType,
        IEnumerable<FormOptionViewModel> options,
        object? initialValue = null)
        => new(label, valueType, initialValue, false, options);

    private void ApplyLookupFilter()
    {
        FilteredLookupOptions.Clear();

        var query = LookupText?.Trim();
        var items = string.IsNullOrWhiteSpace(query)
            ? LookupOptions
            : LookupOptions.Where(x => x.Label.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var item in items)
        {
            FilteredLookupOptions.Add(item);
        }
    }
}
