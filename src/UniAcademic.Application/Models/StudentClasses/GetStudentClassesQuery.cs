using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.StudentClasses;

public sealed class GetStudentClassesQuery
{
    public string? Keyword { get; set; }

    public Guid? FacultyId { get; set; }

    public int? IntakeYear { get; set; }

    public StudentClassStatus? Status { get; set; }
}
