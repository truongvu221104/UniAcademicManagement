namespace UniAcademic.Web.Models.StudentPortal;

public sealed class StudentMaterialsOverviewItemViewModel
{
    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public int MaterialCount { get; set; }

    public DateTime? LatestUploadedAtUtc { get; set; }
}
