namespace UniAcademic.Contracts.Rosters;

public sealed class ExamHandoffStatusResponse
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public Guid RosterSnapshotId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; }

    public int? ResponseCode { get; set; }

    public string? ErrorMessage { get; set; }
}
