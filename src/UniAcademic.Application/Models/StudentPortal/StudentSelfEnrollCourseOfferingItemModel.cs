namespace UniAcademic.Application.Models.StudentPortal;

public sealed class StudentSelfEnrollCourseOfferingItemModel
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public int Credits { get; set; }

    public int Capacity { get; set; }

    public int EnrolledCount { get; set; }

    public int DayOfWeek { get; set; }

    public int StartPeriod { get; set; }

    public int EndPeriod { get; set; }

    public bool IsRosterFinalized { get; set; }

    public bool IsAlreadyEnrolled { get; set; }
}
