using UniAcademic.Contracts.Auth;

namespace UniAcademic.AdminApp.Services.Auth;

public interface IAuthApiClient
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
