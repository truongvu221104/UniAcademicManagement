using System.ComponentModel.DataAnnotations;

namespace UniAcademic.Web.Models.Attendance;

public sealed class CreateAttendanceSessionViewModel
{
    [Required]
    public Guid CourseOfferingId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime SessionDate { get; set; } = DateTime.Today;

    [Range(1, int.MaxValue)]
    public int SessionNo { get; set; } = 1;

    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}
