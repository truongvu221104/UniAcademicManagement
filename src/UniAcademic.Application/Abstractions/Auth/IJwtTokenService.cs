using UniAcademic.Domain.Entities.Identity;

namespace UniAcademic.Application.Abstractions.Auth;

public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(
        User user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        Guid sessionId);
}
