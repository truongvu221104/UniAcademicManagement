using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class GradeResult : AuditableEntity, IAuditableEntity
{
    public Guid CourseOfferingId { get; set; }

    public Guid CourseOfferingRosterSnapshotId { get; set; }

    public Guid RosterItemId { get; set; }

    public decimal WeightedFinalScore { get; set; }

    public decimal PassingScore { get; set; }

    public bool IsPassed { get; set; }

    public DateTime CalculatedAtUtc { get; set; }

    public string CalculatedBy { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public CourseOffering? CourseOffering { get; set; }

    public CourseOfferingRosterSnapshot? CourseOfferingRosterSnapshot { get; set; }

    public CourseOfferingRosterItem? RosterItem { get; set; }
}
