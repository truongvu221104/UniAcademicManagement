namespace UniAcademic.Application.Models.StudentPortal;

public sealed class StudentGradeItemModel
{
    public Guid GradeCategoryId { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public decimal MaxScore { get; set; }

    public decimal? Score { get; set; }

    public string? Note { get; set; }
}
