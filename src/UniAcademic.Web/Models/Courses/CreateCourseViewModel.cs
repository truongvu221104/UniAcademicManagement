using System.ComponentModel.DataAnnotations;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Web.Models.Courses;

public sealed class CreateCourseViewModel
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(1, 15)]
    public int Credits { get; set; }

    public Guid? FacultyId { get; set; }

    [Required]
    public CourseStatus Status { get; set; } = CourseStatus.Active;

    public string? Description { get; set; }
}
