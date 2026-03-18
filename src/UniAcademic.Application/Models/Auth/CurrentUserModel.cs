namespace UniAcademic.Application.Models.Auth;

public sealed class CurrentUserModel
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; set; } = [];

    public IReadOnlyCollection<string> Permissions { get; set; } = [];
}
