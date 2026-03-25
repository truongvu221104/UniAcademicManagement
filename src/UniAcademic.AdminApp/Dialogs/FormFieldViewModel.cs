using System.Collections.ObjectModel;
using UniAcademic.AdminApp.Infrastructure;

namespace UniAcademic.AdminApp.Dialogs;

public sealed class FormFieldViewModel : ObservableObject
{
    private string _textValue = string.Empty;
    private bool _boolValue;
    private string? _selectedOption;

    public FormFieldViewModel(string label, Type valueType, object? initialValue = null, bool isMultiline = false)
    {
        Label = label;
        ValueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        IsNullable = Nullable.GetUnderlyingType(valueType) is not null || !valueType.IsValueType;
        IsMultiline = isMultiline;

        if (ValueType == typeof(bool))
        {
            _boolValue = initialValue as bool? ?? false;
        }
        else if (ValueType.IsEnum)
        {
            foreach (var name in Enum.GetNames(ValueType))
            {
                Options.Add(name);
            }

            _selectedOption = initialValue?.ToString() ?? Options.FirstOrDefault();
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

    public bool IsEnum => ValueType.IsEnum;

    public ObservableCollection<string> Options { get; } = [];

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

    public string? SelectedOption
    {
        get => _selectedOption;
        set => SetProperty(ref _selectedOption, value);
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
}
