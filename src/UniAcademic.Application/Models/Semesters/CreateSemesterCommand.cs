using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Semesters;

public sealed class CreateSemesterCommand
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string AcademicYear { get; set; } = string.Empty;

    public int TermNo { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public SemesterStatus Status { get; set; } = SemesterStatus.Active;

    public string? Description { get; set; }
}
