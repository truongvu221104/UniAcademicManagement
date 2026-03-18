using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Faculties;

public sealed class GetFacultiesQuery
{
    public string? Keyword { get; set; }

    public FacultyStatus? Status { get; set; }
}
