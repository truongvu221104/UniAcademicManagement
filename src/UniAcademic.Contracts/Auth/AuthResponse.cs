namespace UniAcademic.Contracts.Auth;

public sealed class AuthResponse
{
    public Guid SessionId { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public DateTime? AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime? RefreshTokenExpiresAtUtc { get; set; }

    public CurrentUserResponse User { get; set; } = new();
}
