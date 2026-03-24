namespace UniAcademic.Contracts.Attendance;

public sealed class AttendanceSessionListItemResponse
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public DateTime SessionDate { get; set; }

    public int SessionNo { get; set; }

    public string? Title { get; set; }

    public int RecordCount { get; set; }
}
