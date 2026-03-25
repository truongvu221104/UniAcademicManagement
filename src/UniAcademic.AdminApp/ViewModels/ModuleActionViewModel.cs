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
        Command = new AsyncRelayCommand(() => _executeAsync(_owner), () => !owner.IsBusy && (_canExecute?.Invoke(owner) ?? true));
    }

    public string Label { get; }

    public AsyncRelayCommand Command { get; }

    public void RaiseCanExecuteChanged() => Command.RaiseCanExecuteChanged();
}
