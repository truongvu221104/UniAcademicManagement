using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Courses;

public sealed class GetCoursesQuery
{
    public string? Keyword { get; set; }

    public Guid? FacultyId { get; set; }

    public CourseStatus? Status { get; set; }
}
