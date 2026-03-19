using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Courses;

public sealed class CourseModel
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Credits { get; set; }

    public Guid? FacultyId { get; set; }

    public string FacultyCode { get; set; } = string.Empty;

    public string FacultyName { get; set; } = string.Empty;

    public CourseStatus Status { get; set; }

    public string? Description { get; set; }
}
