namespace UniAcademic.Application.Models.Auth;

public sealed class AuthResult
{
    public Guid SessionId { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public DateTime? AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime? RefreshTokenExpiresAtUtc { get; set; }

    public CurrentUserModel User { get; set; } = new();
}
