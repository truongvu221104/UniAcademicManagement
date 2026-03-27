using System.ComponentModel.DataAnnotations;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Web.Models.Attendance;

public sealed class UpdateAttendanceRecordsViewModel
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Session Date")]
    public DateTime SessionDate { get; set; }

    [Display(Name = "Session No")]
    public int SessionNo { get; set; }

    [Display(Name = "Session Title")]
    public string? Title { get; set; }

    [Display(Name = "Session Note")]
    public string? Note { get; set; }

    public List<UpdateAttendanceRecordItemViewModel> Records { get; set; } = [];
}

public sealed class UpdateAttendanceRecordItemViewModel
{
    [Required]
    public Guid RosterItemId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentClassName { get; set; } = string.Empty;

    public AttendanceStatus Status { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}
