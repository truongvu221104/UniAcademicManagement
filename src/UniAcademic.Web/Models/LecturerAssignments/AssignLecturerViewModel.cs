using System.ComponentModel.DataAnnotations;

namespace UniAcademic.Web.Models.LecturerAssignments;

public sealed class AssignLecturerViewModel
{
    [Required]
    public Guid CourseOfferingId { get; set; }

    [Required]
    public Guid LecturerProfileId { get; set; }

    public bool IsPrimary { get; set; }
}
