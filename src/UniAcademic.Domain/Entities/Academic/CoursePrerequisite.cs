using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class CoursePrerequisite : AuditableEntity, IAuditableEntity
{
    public Guid CourseId { get; set; }

    public Guid PrerequisiteCourseId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Course? Course { get; set; }

    public Course? PrerequisiteCourse { get; set; }
}
