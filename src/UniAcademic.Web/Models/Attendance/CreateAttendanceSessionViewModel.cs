using System.ComponentModel.DataAnnotations;

namespace UniAcademic.Web.Models.Attendance;

public sealed class CreateAttendanceSessionViewModel
{
    [Required]
    public Guid CourseOfferingId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Session Date")]
    public DateTime SessionDate { get; set; } = DateTime.Today;

    [Range(1, int.MaxValue)]
    [Display(Name = "Session No")]
    public int SessionNo { get; set; } = 1;

    [StringLength(200)]
    [Display(Name = "Session Title")]
    public string? Title { get; set; }

    [StringLength(1000)]
    [Display(Name = "Session Note")]
    public string? Note { get; set; }
}
