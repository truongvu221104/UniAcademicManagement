namespace UniAcademic.AdminApp.Dialogs;

public sealed class FormOptionViewModel
{
    public FormOptionViewModel(string label, object? value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    public object? Value { get; }

    public override string ToString() => Label;
}
