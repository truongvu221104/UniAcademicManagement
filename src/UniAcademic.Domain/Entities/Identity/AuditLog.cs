using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Identity;

public sealed class AuditLog : AuditableEntity, IAuditableEntity
{
    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? DetailJson { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public User? User { get; set; }
}
