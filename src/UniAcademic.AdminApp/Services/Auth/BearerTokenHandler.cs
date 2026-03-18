using System.Net.Http;
using System.Net.Http.Headers;

namespace UniAcademic.AdminApp.Services.Auth;

public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IAuthSessionService _authSessionService;

    public BearerTokenHandler(IAuthSessionService authSessionService)
    {
        _authSessionService = authSessionService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var current = await _authSessionService.GetCurrentAsync(cancellationToken);

        if (current is not null && current.AccessTokenExpiresAtUtc <= DateTime.UtcNow.AddMinutes(1))
        {
            current = await _authSessionService.RefreshAsync(cancellationToken);
        }

        if (current is not null && !string.IsNullOrWhiteSpace(current.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
