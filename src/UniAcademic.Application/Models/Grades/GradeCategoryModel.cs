namespace UniAcademic.Application.Models.Grades;

public sealed class GradeCategoryModel
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public Guid CourseOfferingRosterSnapshotId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public decimal MaxScore { get; set; }

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; }

    public int EntryCount { get; set; }

    public IReadOnlyCollection<GradeEntryModel> Entries { get; set; } = [];
}
