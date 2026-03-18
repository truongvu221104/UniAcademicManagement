using UniAcademic.Contracts.Auth;

namespace UniAcademic.AdminApp.Services.Auth;

public sealed class AuthTokenSnapshot
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    public CurrentUserResponse? User { get; set; }
}
