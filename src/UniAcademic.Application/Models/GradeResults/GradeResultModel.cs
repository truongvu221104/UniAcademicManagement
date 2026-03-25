namespace UniAcademic.Application.Models.GradeResults;

public sealed class GradeResultModel
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public Guid CourseOfferingRosterSnapshotId { get; set; }

    public Guid RosterItemId { get; set; }

    public Guid StudentProfileId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentClassName { get; set; } = string.Empty;

    public decimal WeightedFinalScore { get; set; }

    public decimal PassingScore { get; set; }

    public bool IsPassed { get; set; }

    public DateTime CalculatedAtUtc { get; set; }

    public string CalculatedBy { get; set; } = string.Empty;
}
