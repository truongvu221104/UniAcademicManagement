namespace UniAcademic.Application.Models.StudentPortal;

public sealed class StudentCurrentEnrollmentItemModel
{
    public Guid EnrollmentId { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public int Credits { get; set; }

    public int DayOfWeek { get; set; }

    public int StartPeriod { get; set; }

    public int EndPeriod { get; set; }

    public bool IsRosterFinalized { get; set; }

    public DateTime EnrolledAtUtc { get; set; }

    public string? Note { get; set; }
}
