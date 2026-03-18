using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Identity;

public sealed class RefreshToken : AuditableEntity, IAuditableEntity
{
    public Guid UserId { get; set; }

    public Guid UserSessionId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? RevokedByIp { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? CreatedByIp { get; set; }

    public string? UserAgent { get; set; }

    public User User { get; set; } = null!;

    public UserSession UserSession { get; set; } = null!;
}
