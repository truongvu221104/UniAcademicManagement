namespace UniAcademic.Application.Models.ExamHandoff;

public sealed class ExamHandoffLogModel
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public Guid RosterSnapshotId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; }

    public int? ResponseCode { get; set; }

    public string? ErrorMessage { get; set; }
}
