namespace UniAcademic.Application.Models.Auth;

public sealed class AuthLogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
