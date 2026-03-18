using System.ComponentModel.DataAnnotations;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Web.Models.Faculties;

public sealed class UpdateFacultyViewModel
{
    public Guid Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public FacultyStatus Status { get; set; } = FacultyStatus.Active;
}
