namespace UniAcademic.Application.Models.Auth;

public sealed class AuthForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}
