using UniAcademic.Domain.Enums;

namespace UniAcademic.Contracts.Semesters;

public sealed class UpdateSemesterRequest
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
