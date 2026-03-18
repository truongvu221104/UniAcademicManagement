namespace UniAcademic.Infrastructure.Options;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string Username { get; set; } = "admin";

    public string Email { get; set; } = "admin@uniacademic.local";

    public string DisplayName { get; set; } = "System Administrator";

    public string Password { get; set; } = "Admin@123456";
}
