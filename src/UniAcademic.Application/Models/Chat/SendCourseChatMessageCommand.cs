namespace UniAcademic.Application.Models.Chat;

public sealed class SendCourseChatMessageCommand
{
    public Guid CourseOfferingId { get; set; }

    public string MessageText { get; set; } = string.Empty;
}
