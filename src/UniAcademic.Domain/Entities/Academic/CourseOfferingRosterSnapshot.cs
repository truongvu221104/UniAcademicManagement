using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class CourseOfferingRosterSnapshot : AuditableEntity, IAuditableEntity
{
    public Guid CourseOfferingId { get; set; }

    public DateTime FinalizedAtUtc { get; set; }

    public string FinalizedBy { get; set; } = string.Empty;

    public int ItemCount { get; set; }

    public string? Note { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public CourseOffering? CourseOffering { get; set; }

    public ICollection<CourseOfferingRosterItem> Items { get; set; } = [];
}
