using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.StudentPortal;

public sealed class StudentAttendanceItemModel
{
    public Guid AttendanceSessionId { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public DateTime SessionDate { get; set; }

    public int SessionNo { get; set; }

    public string? Title { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? Note { get; set; }
}
