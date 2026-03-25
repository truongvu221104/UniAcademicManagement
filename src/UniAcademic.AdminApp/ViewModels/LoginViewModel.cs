using UniAcademic.AdminApp.Commands;
using UniAcademic.AdminApp.Dialogs;
using UniAcademic.AdminApp.Infrastructure;
using UniAcademic.AdminApp.Services.Auth;
using UniAcademic.Contracts.Auth;

namespace UniAcademic.AdminApp.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IMessageDialogService _messageDialogService;
    private string _userNameOrEmail = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    public LoginViewModel(IAuthSessionService authSessionService, IMessageDialogService messageDialogService)
    {
        _authSessionService = authSessionService;
        _messageDialogService = messageDialogService;
        LoginCommand = new AsyncRelayCommand(LoginAsync, () => !IsBusy);
    }

    public event EventHandler<AuthTokenSnapshot>? LoggedIn;

    public string UserNameOrEmail
    {
        get => _userNameOrEmail;
        set => SetProperty(ref _userNameOrEmail, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                LoginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand LoginCommand { get; }

    public async Task TryRestoreSessionAsync()
    {
        var snapshot = await _authSessionService.GetCurrentAsync();
        if (snapshot?.User is not null)
        {
            LoggedIn?.Invoke(this, snapshot);
        }
    }

    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(UserNameOrEmail) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username/email and password are required.";
            return;
        }

        try
        {
            IsBusy = true;
            var snapshot = await _authSessionService.LoginAsync(new LoginRequest
            {
                UserNameOrEmail = UserNameOrEmail.Trim(),
                Password = Password,
                RememberMe = RememberMe,
                DeviceName = Environment.MachineName
            });

            Password = string.Empty;
            LoggedIn?.Invoke(this, snapshot);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _messageDialogService.ShowError(ex.Message, "Login Failed");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
