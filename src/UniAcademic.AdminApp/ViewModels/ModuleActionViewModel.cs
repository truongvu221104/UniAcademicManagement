using UniAcademic.AdminApp.Commands;
using UniAcademic.AdminApp.Infrastructure;

namespace UniAcademic.AdminApp.ViewModels;

public sealed class ModuleActionViewModel : ObservableObject
{
    private readonly ModulePageViewModel _owner;
    private readonly Func<ModulePageViewModel, Task> _executeAsync;
    private readonly Func<ModulePageViewModel, bool>? _canExecute;

    public ModuleActionViewModel(string label, ModulePageViewModel owner, Func<ModulePageViewModel, Task> executeAsync, Func<ModulePageViewModel, bool>? canExecute = null)
    {
        Label = label;
        _owner = owner;
        _executeAsync = executeAsync;
        _canExecute = canExecute;
        Kind = InferKind(label);
        Command = new AsyncRelayCommand(
            () => _executeAsync(_owner),
            () => !owner.IsBusy && (_canExecute?.Invoke(owner) ?? true),
            ex => _owner.NotifyError(ex.Message));
    }

    public string Label { get; }

    public ModuleActionKind Kind { get; }

    public AsyncRelayCommand Command { get; }

    public void RaiseCanExecuteChanged() => Command.RaiseCanExecuteChanged();

    private static ModuleActionKind InferKind(string label)
    {
        if (label.Contains("Delete", StringComparison.OrdinalIgnoreCase)
            || label.Contains("Unassign", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleActionKind.Danger;
        }

        if (label.Contains("Reopen", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleActionKind.Warning;
        }

        if (label.StartsWith("Create", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("Enroll", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("Finalize", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("Calculate", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("Upload", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("Assign", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("Create Session", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("Create Category", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleActionKind.Primary;
        }

        if (label.Contains("Publish", StringComparison.OrdinalIgnoreCase)
            || label.Contains("Retry", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleActionKind.Warning;
        }

        return ModuleActionKind.Secondary;
    }
}

public enum ModuleActionKind
{
    Secondary = 0,
    Primary = 1,
    Warning = 2,
    Danger = 3
}
