namespace UniAcademic.Application.Models.Chat;

public sealed class CourseChatConversationModel
{
    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public Guid CurrentUserId { get; set; }

    public string CurrentUserDisplayName { get; set; } = string.Empty;

    public string CurrentUserRole { get; set; } = string.Empty;

    public IReadOnlyCollection<CourseChatMessageModel> Messages { get; set; } = [];
}
