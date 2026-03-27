namespace UniAcademic.Web.Models.LecturerPortal;

public sealed class LecturerAttendanceOfferingOverviewItemViewModel
{
    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int SessionCount { get; set; }

    public DateTime? LatestSessionDate { get; set; }
}
