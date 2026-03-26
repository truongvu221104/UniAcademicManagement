using UniAcademic.Domain.Enums;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class Course : AuditableEntity, IAuditableEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Credits { get; set; }

    public Guid? FacultyId { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.Active;

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public string? DeletedBy { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Faculty? Faculty { get; set; }

    public ICollection<CoursePrerequisite> Prerequisites { get; set; } = [];

    public ICollection<CoursePrerequisite> RequiredForCourses { get; set; } = [];
}
