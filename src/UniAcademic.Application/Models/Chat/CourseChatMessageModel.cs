namespace UniAcademic.Application.Models.Chat;

public sealed class CourseChatMessageModel
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public Guid SenderUserId { get; set; }

    public string SenderDisplayName { get; set; } = string.Empty;

    public string SenderRole { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; }

    public bool IsMine { get; set; }
}
