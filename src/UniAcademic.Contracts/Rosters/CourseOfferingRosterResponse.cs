namespace UniAcademic.Contracts.Rosters;

public sealed class CourseOfferingRosterResponse
{
    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public bool IsFinalized { get; set; }

    public DateTime? FinalizedAtUtc { get; set; }

    public string? FinalizedBy { get; set; }

    public int ItemCount { get; set; }

    public string? Note { get; set; }

    public IReadOnlyCollection<CourseOfferingRosterItemResponse> Items { get; set; } = [];
}
