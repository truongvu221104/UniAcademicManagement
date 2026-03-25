using System.Collections.ObjectModel;
using UniAcademic.AdminApp.Infrastructure;

namespace UniAcademic.AdminApp.ViewModels;

public sealed class ModulePageViewModel : ObservableObject
{
    private readonly Func<CancellationToken, Task<IReadOnlyCollection<object>>> _loadListAsync;
    private readonly Func<object, CancellationToken, Task<object?>>? _loadDetailAsync;
    private object? _selectedItem;
    private string _detailsText = string.Empty;
    private string _statusMessage = string.Empty;
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

    public ObservableCollection<ModuleActionViewModel> Actions { get; } = [];

    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
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

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

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

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading...";
            var items = await _loadListAsync(cancellationToken);
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            StatusMessage = $"Loaded {Items.Count} item(s).";
            if (SelectedItem is null && Items.Count > 0)
            {
                SelectedItem = Items[0];
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

    public void NotifySuccess(string message) => StatusMessage = message;

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
            return;
        }

        var detail = _loadDetailAsync is null
            ? SelectedItem
            : await _loadDetailAsync(SelectedItem, cancellationToken) ?? SelectedItem;

        DetailsText = JsonFormatter.Format(detail);
    }
}
