using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class GradeEntry : AuditableEntity, IAuditableEntity
{
    public Guid GradeCategoryId { get; set; }

    public Guid RosterItemId { get; set; }

    public decimal? Score { get; set; }

    public string? Note { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public GradeCategory? GradeCategory { get; set; }

    public CourseOfferingRosterItem? RosterItem { get; set; }
}
