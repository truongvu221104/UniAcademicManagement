using UniAcademic.Domain.Enums;

namespace UniAcademic.Contracts.StudentClasses;

public sealed class StudentClassListItemResponse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid FacultyId { get; set; }

    public string FacultyName { get; set; } = string.Empty;

    public int IntakeYear { get; set; }

    public StudentClassStatus Status { get; set; }
}
