using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Courses;

public sealed class UpdateCourseCommand
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Credits { get; set; }

    public Guid? FacultyId { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.Active;

    public string? Description { get; set; }
}
