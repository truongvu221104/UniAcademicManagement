namespace UniAcademic.Contracts.Auth;

public sealed class LoginRequest
{
    public string UserNameOrEmail { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string ClientType { get; set; } = "Api";

    public string? DeviceName { get; set; }
}
