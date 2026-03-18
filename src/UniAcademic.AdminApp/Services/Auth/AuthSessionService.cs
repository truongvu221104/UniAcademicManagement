using UniAcademic.Contracts.Auth;

namespace UniAcademic.AdminApp.Services.Auth;

public sealed class AuthSessionService : IAuthSessionService
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ITokenStore _tokenStore;

    public AuthSessionService(IAuthApiClient authApiClient, ITokenStore tokenStore)
    {
        _authApiClient = authApiClient;
        _tokenStore = tokenStore;
    }

    public Task<AuthTokenSnapshot?> GetCurrentAsync(CancellationToken cancellationToken = default)
        => _tokenStore.LoadAsync(cancellationToken);

    public async Task<AuthTokenSnapshot> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _authApiClient.LoginAsync(request, cancellationToken);
        var snapshot = Map(response);
        await _tokenStore.SaveAsync(snapshot, cancellationToken);
        return snapshot;
    }

    public async Task<AuthTokenSnapshot?> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var current = await _tokenStore.LoadAsync(cancellationToken);
        if (current is null || string.IsNullOrWhiteSpace(current.RefreshToken))
        {
            return null;
        }

        var response = await _authApiClient.RefreshAsync(new RefreshTokenRequest
        {
            RefreshToken = current.RefreshToken
        }, cancellationToken);

        var snapshot = Map(response);
        await _tokenStore.SaveAsync(snapshot, cancellationToken);
        return snapshot;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var current = await _tokenStore.LoadAsync(cancellationToken);
        if (current is not null && !string.IsNullOrWhiteSpace(current.RefreshToken))
        {
            await _authApiClient.LogoutAsync(new LogoutRequest
            {
                RefreshToken = current.RefreshToken
            }, cancellationToken);
        }

        await _tokenStore.ClearAsync(cancellationToken);
    }

    private static AuthTokenSnapshot Map(AuthResponse response)
    {
        return new AuthTokenSnapshot
        {
            AccessToken = response.AccessToken,
            AccessTokenExpiresAtUtc = response.AccessTokenExpiresAtUtc ?? DateTime.MinValue,
            RefreshToken = response.RefreshToken,
            RefreshTokenExpiresAtUtc = response.RefreshTokenExpiresAtUtc ?? DateTime.MinValue,
            User = response.User
        };
    }
}
