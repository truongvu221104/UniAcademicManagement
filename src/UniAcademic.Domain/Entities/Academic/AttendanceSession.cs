using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class AttendanceSession : AuditableEntity, IAuditableEntity
{
    public Guid CourseOfferingId { get; set; }

    public Guid CourseOfferingRosterSnapshotId { get; set; }

    public DateTime SessionDate { get; set; }

    public int SessionNo { get; set; }

    public string? Title { get; set; }

    public string? Note { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public CourseOffering? CourseOffering { get; set; }

    public CourseOfferingRosterSnapshot? CourseOfferingRosterSnapshot { get; set; }

    public ICollection<AttendanceRecord> Records { get; set; } = [];
}
