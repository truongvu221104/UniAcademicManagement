using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Identity;

public sealed class User : AuditableEntity, IAuditableEntity
{
    public string Username { get; set; } = string.Empty;

    public string NormalizedUsername { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsLocked { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockoutEndUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
