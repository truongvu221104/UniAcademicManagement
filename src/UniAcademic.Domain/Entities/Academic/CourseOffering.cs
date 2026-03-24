using UniAcademic.Domain.Enums;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class CourseOffering : AuditableEntity, IAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid CourseId { get; set; }

    public Guid SemesterId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public CourseOfferingStatus Status { get; set; } = CourseOfferingStatus.Active;

    public bool IsRosterFinalized { get; set; }

    public DateTime? RosterFinalizedAtUtc { get; set; }

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public string? DeletedBy { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Course? Course { get; set; }

    public Semester? Semester { get; set; }
}
