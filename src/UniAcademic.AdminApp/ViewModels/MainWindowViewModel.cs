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
    private string _currentUserRoleSummary = "Guest";
    private string _currentSectionTitle = "Workspace";
    private string _currentModuleTitle = "Welcome";
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

    public string CurrentUserRoleSummary
    {
        get => _currentUserRoleSummary;
        set => SetProperty(ref _currentUserRoleSummary, value);
    }

    public string CurrentSectionTitle
    {
        get => _currentSectionTitle;
        set => SetProperty(ref _currentSectionTitle, value);
    }

    public string CurrentModuleTitle
    {
        get => _currentModuleTitle;
        set => SetProperty(ref _currentModuleTitle, value);
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
        SetSelectedNavigation(definition);
        var module = definition.Create();
        CurrentModule = module;
        CurrentModuleTitle = definition.Title;
        await module.RefreshAsync();
    }

    private void OnLoggedIn(object? sender, AuthTokenSnapshot snapshot)
    {
        CurrentUserDisplay = $"{snapshot.User?.DisplayName} ({snapshot.User?.Username})";
        CurrentUserRoleSummary = snapshot.User?.Roles is { Count: > 0 }
            ? string.Join(" / ", snapshot.User.Roles)
            : "Authenticated";
        IsAuthenticated = true;
        BuildNavigation(snapshot.User?.Username);
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

    private void BuildNavigation(string? username)
    {
        Groups.Clear();
        foreach (var group in _moduleCatalog.GetModules(username).GroupBy(x => x.Group))
        {
            Groups.Add(new NavigationGroupViewModel(group.Key, group.Select(def => new NavigationItemViewModel(def, this)).ToList()));
        }
    }

    private void SetSelectedNavigation(ModuleDefinition definition)
    {
        foreach (var item in Groups.SelectMany(x => x.Items))
        {
            item.IsSelected = ReferenceEquals(item.Definition, definition);
            if (item.IsSelected)
            {
                CurrentSectionTitle = item.GroupTitle;
            }
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
        CurrentUserRoleSummary = "Guest";
        CurrentSectionTitle = "Workspace";
        CurrentModuleTitle = "Welcome";
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

public sealed class NavigationItemViewModel : ObservableObject
{
    private bool _isSelected;

    public NavigationItemViewModel(ModuleDefinition definition, MainWindowViewModel mainWindowViewModel)
    {
        Definition = definition;
        OpenCommand = new AsyncRelayCommand(() => mainWindowViewModel.OpenModuleAsync(definition));
    }

    public ModuleDefinition Definition { get; }

    public string Title => Definition.Title;

    public string GroupTitle => Definition.Group;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public AsyncRelayCommand OpenCommand { get; }
}
