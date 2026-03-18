using UniAcademic.Application.Models.Auth;

namespace UniAcademic.Application.Abstractions.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> RefreshAsync(AuthRefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(AuthLogoutRequest request, CancellationToken cancellationToken = default);

    Task LogoutCurrentSessionAsync(CancellationToken cancellationToken = default);

    Task LogoutAllAsync(CancellationToken cancellationToken = default);

    Task<CurrentUserModel> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetMyPermissionsAsync(CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(AuthChangePasswordRequest request, CancellationToken cancellationToken = default);
}
