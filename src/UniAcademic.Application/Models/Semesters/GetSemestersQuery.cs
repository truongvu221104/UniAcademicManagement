using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Semesters;

public sealed class GetSemestersQuery
{
    public string? Keyword { get; set; }

    public string? AcademicYear { get; set; }

    public int? TermNo { get; set; }

    public SemesterStatus? Status { get; set; }
}
