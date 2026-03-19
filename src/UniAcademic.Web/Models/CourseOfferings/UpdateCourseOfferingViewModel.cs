using System.ComponentModel.DataAnnotations;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Web.Models.CourseOfferings;

public sealed class UpdateCourseOfferingViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    public Guid SemesterId { get; set; }

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [Range(1, 500)]
    public int Capacity { get; set; }

    [Required]
    public CourseOfferingStatus Status { get; set; } = CourseOfferingStatus.Active;

    public string? Description { get; set; }
}
