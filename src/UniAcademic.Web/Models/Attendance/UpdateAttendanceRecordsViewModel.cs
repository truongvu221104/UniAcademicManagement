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
    public DateTime SessionDate { get; set; }

    public int SessionNo { get; set; }

    public string? Title { get; set; }

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
