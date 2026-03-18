using UniAcademic.Domain.Enums;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class Faculty : AuditableEntity, IAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public FacultyStatus Status { get; set; } = FacultyStatus.Active;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public string? DeletedBy { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
