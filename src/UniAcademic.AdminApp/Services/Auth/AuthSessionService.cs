using UniAcademic.Contracts.Auth;
using System.Net;

namespace UniAcademic.AdminApp.Services.Auth;

public sealed class AuthSessionService : IAuthSessionService
{
    private const string SuperAdminRole = "SuperAdmin";
    private const string StaffRole = "Staff";
    private const string AdminAppAccessDeniedMessage = "This account is not allowed to sign in to AdminApp.";

    private readonly IAuthApiClient _authApiClient;
    private readonly ITokenStore _tokenStore;

    public AuthSessionService(IAuthApiClient authApiClient, ITokenStore tokenStore)
    {
        _authApiClient = authApiClient;
        _tokenStore = tokenStore;
    }

    public async Task<AuthTokenSnapshot?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _tokenStore.LoadAsync(cancellationToken);
        if (snapshot?.User is null)
        {
            return snapshot;
        }

        if (HasAdminAppAccess(snapshot.User))
        {
            return snapshot;
        }

        await _tokenStore.ClearAsync(cancellationToken);
        return null;
    }

    public async Task<AuthTokenSnapshot> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _authApiClient.LoginAsync(request, cancellationToken);
        EnsureAdminAppAccess(response.User);
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

        AuthResponse response;
        try
        {
            response = await _authApiClient.RefreshAsync(new RefreshTokenRequest
            {
                RefreshToken = current.RefreshToken
            }, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _tokenStore.ClearAsync(cancellationToken);
            return null;
        }

        if (!HasAdminAppAccess(response.User))
        {
            await _tokenStore.ClearAsync(cancellationToken);
            return null;
        }

        var snapshot = Map(response);
        await _tokenStore.SaveAsync(snapshot, cancellationToken);
        return snapshot;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var current = await _tokenStore.LoadAsync(cancellationToken);
        if (current is not null && !string.IsNullOrWhiteSpace(current.RefreshToken))
        {
            try
            {
                await _authApiClient.LogoutAsync(new LogoutRequest
                {
                    RefreshToken = current.RefreshToken
                }, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Treat stale refresh tokens as already logged out.
            }
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

    private static void EnsureAdminAppAccess(CurrentUserResponse user)
    {
        if (!HasAdminAppAccess(user))
        {
            throw new InvalidOperationException(AdminAppAccessDeniedMessage);
        }
    }

    private static bool HasAdminAppAccess(CurrentUserResponse? user)
    {
        if (user?.Roles is null || user.Roles.Count == 0)
        {
            return false;
        }

        return user.Roles.Contains(SuperAdminRole, StringComparer.OrdinalIgnoreCase)
            || user.Roles.Contains(StaffRole, StringComparer.OrdinalIgnoreCase);
    }
}
