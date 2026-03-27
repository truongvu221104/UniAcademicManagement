using UniAcademic.Application.Models.StudentPortal;

namespace UniAcademic.Web.Models.StudentPortal;

public sealed class StudentGradeDetailsViewModel
{
    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public IReadOnlyCollection<StudentGradeItemModel> GradeItems { get; set; } = [];

    public decimal? FinalScore { get; set; }

    public decimal? PassingScore { get; set; }

    public bool? IsPassed { get; set; }

    public DateTime? CalculatedAtUtc { get; set; }
}
