using System.ComponentModel.DataAnnotations;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Web.Models.StudentClasses;

public sealed class CreateStudentClassViewModel
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid FacultyId { get; set; }

    [Range(2000, 3000)]
    public int IntakeYear { get; set; }

    [Required]
    public StudentClassStatus Status { get; set; } = StudentClassStatus.Active;

    public string? Description { get; set; }
}
