using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Security;

namespace UniAcademic.Infrastructure.Security;

public sealed class CurrentUserAccessor : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId => Guid.TryParse(_httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var value)
        ? value
        : null;

    public string? Username => _httpContextAccessor.HttpContext?.User.Identity?.Name;

    public Guid? SessionId => Guid.TryParse(_httpContextAccessor.HttpContext?.User.FindFirstValue(AppClaimTypes.SessionId), out var value)
        ? value
        : null;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Permissions => _httpContextAccessor.HttpContext?.User.FindAll(AppClaimTypes.Permission)
        .Select(static claim => claim.Value)
        .ToArray() ?? [];
}
