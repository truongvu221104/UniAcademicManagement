namespace UniAcademic.Web.Models.StudentPortal;

public sealed class StudentGradeOverviewItemViewModel
{
    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public int GradeItemCount { get; set; }

    public int ScoredItemCount { get; set; }

    public int PendingItemCount { get; set; }

    public decimal AverageScore { get; set; }

    public decimal? FinalScore { get; set; }

    public decimal? PassingScore { get; set; }

    public bool? IsPassed { get; set; }

    public DateTime? CalculatedAtUtc { get; set; }
}
