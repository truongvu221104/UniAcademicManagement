namespace UniAcademic.Application.Models.Chat;

public sealed class CourseChatRoomItemModel
{
    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int MessageCount { get; set; }

    public string? LatestMessagePreview { get; set; }

    public string? LatestSenderDisplayName { get; set; }

    public DateTime? LatestMessageAtUtc { get; set; }
}
