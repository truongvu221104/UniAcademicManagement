using UniAcademic.Domain.Enums;

namespace UniAcademic.Contracts.Faculties;

public sealed class FacultyListItemResponse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public FacultyStatus Status { get; set; }
}
