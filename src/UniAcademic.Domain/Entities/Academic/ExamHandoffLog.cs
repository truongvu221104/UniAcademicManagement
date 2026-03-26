using UniAcademic.Domain.Enums;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class ExamHandoffLog : AuditableEntity, IAuditableEntity
{
    public Guid CourseOfferingId { get; set; }

    public Guid RosterSnapshotId { get; set; }

    public ExamHandoffStatus Status { get; set; } = ExamHandoffStatus.Pending;

    public DateTime SentAtUtc { get; set; }

    public int? ResponseCode { get; set; }

    public string? ErrorMessage { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public CourseOffering? CourseOffering { get; set; }

    public CourseOfferingRosterSnapshot? RosterSnapshot { get; set; }
}
