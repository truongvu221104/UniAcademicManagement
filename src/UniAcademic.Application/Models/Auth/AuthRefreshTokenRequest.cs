namespace UniAcademic.Application.Models.Auth;

public sealed class AuthRefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
