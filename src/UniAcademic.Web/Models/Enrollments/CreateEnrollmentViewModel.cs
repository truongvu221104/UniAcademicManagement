using System.ComponentModel.DataAnnotations;

namespace UniAcademic.Web.Models.Enrollments;

public sealed class CreateEnrollmentViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required]
    public Guid CourseOfferingId { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}
