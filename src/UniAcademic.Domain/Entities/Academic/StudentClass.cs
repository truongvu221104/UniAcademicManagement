using UniAcademic.Domain.Enums;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class StudentClass : AuditableEntity, IAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid FacultyId { get; set; }

    public int IntakeYear { get; set; }

    public StudentClassStatus Status { get; set; } = StudentClassStatus.Active;

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public string? DeletedBy { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Faculty? Faculty { get; set; }
}
