using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Identity;

public sealed class UserSession : AuditableEntity, IAuditableEntity
{
    public Guid UserId { get; set; }

    public string ClientType { get; set; } = string.Empty;

    public string? DeviceName { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime LastSeenAtUtc { get; set; }

    public DateTime? EndedAtUtc { get; set; }

    public bool IsRevoked { get; set; }

    public User User { get; set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
