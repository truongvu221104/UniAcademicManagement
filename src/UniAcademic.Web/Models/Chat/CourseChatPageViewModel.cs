using UniAcademic.Application.Models.Chat;

namespace UniAcademic.Web.Models.Chat;

public sealed class CourseChatPageViewModel
{
    public string RoomArea { get; set; } = string.Empty;

    public string RoomController { get; set; } = string.Empty;

    public string EmptyStateTitle { get; set; } = "Messages";

    public string EmptyStateDescription { get; set; } = "Choose a class to start the conversation.";

    public IReadOnlyCollection<CourseChatRoomItemModel> Rooms { get; set; } = [];

    public CourseChatConversationModel? Conversation { get; set; }
}
