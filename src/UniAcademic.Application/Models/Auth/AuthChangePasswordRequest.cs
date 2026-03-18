namespace UniAcademic.Application.Models.Auth;

public sealed class AuthChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
