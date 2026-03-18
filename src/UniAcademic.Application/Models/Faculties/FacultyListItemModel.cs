using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Faculties;

public sealed class FacultyListItemModel
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public FacultyStatus Status { get; set; }
}
