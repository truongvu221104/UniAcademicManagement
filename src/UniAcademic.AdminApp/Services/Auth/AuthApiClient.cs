using System.Net.Http;
using System.Net.Http.Json;
using UniAcademic.Contracts.Auth;

namespace UniAcademic.AdminApp.Services.Auth;

public sealed class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        request.ClientType = "Wpf";
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/logout", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<CurrentUserResponse>("api/auth/me", cancellationToken))!;
    }
}
