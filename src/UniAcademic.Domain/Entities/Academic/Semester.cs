using UniAcademic.Domain.Enums;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class Semester : AuditableEntity, IAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string AcademicYear { get; set; } = string.Empty;

    public int TermNo { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public SemesterStatus Status { get; set; } = SemesterStatus.Active;

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public string? DeletedBy { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
