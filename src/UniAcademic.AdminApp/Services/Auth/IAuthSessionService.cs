using UniAcademic.Contracts.Auth;

namespace UniAcademic.AdminApp.Services.Auth;

public interface IAuthSessionService
{
    Task<AuthTokenSnapshot?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<AuthTokenSnapshot> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthTokenSnapshot?> RefreshAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}
