using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class CourseOfferingRosterItem : AuditableEntity, IAuditableEntity
{
    public Guid RosterSnapshotId { get; set; }

    public Guid EnrollmentId { get; set; }

    public Guid StudentProfileId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentClassName { get; set; } = string.Empty;

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public CourseOfferingRosterSnapshot? RosterSnapshot { get; set; }
}
