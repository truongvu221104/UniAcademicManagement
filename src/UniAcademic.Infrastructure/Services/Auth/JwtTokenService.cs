using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Infrastructure.Options;

namespace UniAcademic.Infrastructure.Services.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(
        User user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        Guid sessionId)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(AppClaimTypes.DisplayName, user.DisplayName),
            new(AppClaimTypes.SessionId, sessionId.ToString())
        };

        claims.AddRange(roles.Select(static role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(static permission => new Claim(AppClaimTypes.Permission, permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
