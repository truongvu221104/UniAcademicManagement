using System.ComponentModel.DataAnnotations;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Web.Models.Semesters;

public sealed class CreateSemesterViewModel
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string AcademicYear { get; set; } = string.Empty;

    [Range(1, 3)]
    public int TermNo { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Required]
    public SemesterStatus Status { get; set; } = SemesterStatus.Active;

    public string? Description { get; set; }
}
