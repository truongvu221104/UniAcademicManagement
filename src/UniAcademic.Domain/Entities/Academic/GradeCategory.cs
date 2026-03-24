using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class GradeCategory : AuditableEntity, IAuditableEntity
{
    public Guid CourseOfferingId { get; set; }

    public Guid CourseOfferingRosterSnapshotId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public decimal MaxScore { get; set; }

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;

    public byte[] RowVersion { get; set; } = [];

    public CourseOffering? CourseOffering { get; set; }

    public CourseOfferingRosterSnapshot? CourseOfferingRosterSnapshot { get; set; }

    public ICollection<GradeEntry> Entries { get; set; } = [];
}
