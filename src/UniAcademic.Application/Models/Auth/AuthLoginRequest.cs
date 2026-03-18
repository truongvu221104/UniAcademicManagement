namespace UniAcademic.Application.Models.Auth;

public sealed class AuthLoginRequest
{
    public string UserNameOrEmail { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string ClientType { get; set; } = "Api";

    public string? DeviceName { get; set; }

    public bool IssueRefreshToken { get; set; } = true;
}
