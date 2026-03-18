using System.Security.Cryptography;
using System.Text;
using UniAcademic.Application.Abstractions.Auth;

namespace UniAcademic.Infrastructure.Services.Auth;

public sealed class RefreshTokenService : IRefreshTokenService
{
    public string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
