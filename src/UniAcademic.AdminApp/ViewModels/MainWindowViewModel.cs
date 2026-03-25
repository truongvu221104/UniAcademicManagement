using System.Collections.ObjectModel;
using UniAcademic.AdminApp.Commands;
using UniAcademic.AdminApp.Dialogs;
using UniAcademic.AdminApp.Infrastructure;
using UniAcademic.AdminApp.Navigation;
using UniAcademic.AdminApp.Services.Auth;

namespace UniAcademic.AdminApp.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly AdminModuleCatalog _moduleCatalog;
    private readonly LoginViewModel _loginViewModel;
    private ModulePageViewModel? _currentModule;
    private string _currentUserDisplay = "Not signed in";
    private bool _isAuthenticated;

    public MainWindowViewModel(
        IAuthSessionService authSessionService,
        IMessageDialogService messageDialogService,
        AdminModuleCatalog moduleCatalog,
        LoginViewModel loginViewModel)
    {
        _authSessionService = authSessionService;
        _messageDialogService = messageDialogService;
        _moduleCatalog = moduleCatalog;
        _loginViewModel = loginViewModel;
        _loginViewModel.LoggedIn += OnLoggedIn;

        LogoutCommand = new AsyncRelayCommand(LogoutAsync, () => IsAuthenticated);
    }

    public ObservableCollection<NavigationGroupViewModel> Groups { get; } = [];

    public LoginViewModel Login => _loginViewModel;

    public ModulePageViewModel? CurrentModule
    {
        get => _currentModule;
        set => SetProperty(ref _currentModule, value);
    }

    public string CurrentUserDisplay
    {
        get => _currentUserDisplay;
        set => SetProperty(ref _currentUserDisplay, value);
    }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set
        {
            if (SetProperty(ref _isAuthenticated, value))
            {
                LogoutCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand LogoutCommand { get; }

    public async Task InitializeAsync()
    {
        await _loginViewModel.TryRestoreSessionAsync();
    }

    public async Task OpenModuleAsync(ModuleDefinition definition)
    {
        var module = definition.Create();
        CurrentModule = module;
        await module.RefreshAsync();
    }

    private void OnLoggedIn(object? sender, AuthTokenSnapshot snapshot)
    {
        CurrentUserDisplay = $"{snapshot.User?.DisplayName} ({snapshot.User?.Username})";
        IsAuthenticated = true;
        BuildNavigation();
        _ = OpenFirstModuleAsync();
    }

    private async Task OpenFirstModuleAsync()
    {
        var first = Groups.SelectMany(x => x.Items).FirstOrDefault();
        if (first is not null)
        {
            await OpenModuleAsync(first.Definition);
        }
    }

    private void BuildNavigation()
    {
        Groups.Clear();
        foreach (var group in _moduleCatalog.GetModules().GroupBy(x => x.Group))
        {
            Groups.Add(new NavigationGroupViewModel(group.Key, group.Select(def => new NavigationItemViewModel(def, this)).ToList()));
        }
    }

    private async Task LogoutAsync()
    {
        try
        {
            await _authSessionService.LogoutAsync();
        }
        catch (Exception ex)
        {
            _messageDialogService.ShowError(ex.Message, "Logout Failed");
        }

        Groups.Clear();
        CurrentModule = null;
        CurrentUserDisplay = "Not signed in";
        IsAuthenticated = false;
    }
}

public sealed class NavigationGroupViewModel
{
    public NavigationGroupViewModel(string title, IReadOnlyCollection<NavigationItemViewModel> items)
    {
        Title = title;
        Items = items;
    }

    public string Title { get; }

    public IReadOnlyCollection<NavigationItemViewModel> Items { get; }
}

public sealed class NavigationItemViewModel
{
    public NavigationItemViewModel(ModuleDefinition definition, MainWindowViewModel mainWindowViewModel)
    {
        Definition = definition;
        OpenCommand = new AsyncRelayCommand(() => mainWindowViewModel.OpenModuleAsync(definition));
    }

    public ModuleDefinition Definition { get; }

    public string Title => Definition.Title;

    public AsyncRelayCommand OpenCommand { get; }
}
