using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.StudentClasses;

public sealed class CreateStudentClassCommand
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid FacultyId { get; set; }

    public int IntakeYear { get; set; }

    public StudentClassStatus Status { get; set; } = StudentClassStatus.Active;

    public string? Description { get; set; }
}
