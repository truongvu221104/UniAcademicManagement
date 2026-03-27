using UniAcademic.Application.Models.Chat;

namespace UniAcademic.Application.Abstractions.Chat;

public interface ICourseChatService
{
    Task<IReadOnlyCollection<CourseChatRoomItemModel>> GetMyRoomsAsync(CancellationToken cancellationToken = default);

    Task<CourseChatConversationModel> GetConversationAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);

    Task<CourseChatMessageModel> SendMessageAsync(SendCourseChatMessageCommand command, CancellationToken cancellationToken = default);

    Task<bool> CanAccessCourseOfferingAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);

    Task<CourseChatMessageModel> SendMessageAsync(Guid? userId, string? username, SendCourseChatMessageCommand command, CancellationToken cancellationToken = default);

    Task<bool> CanAccessCourseOfferingAsync(Guid? userId, string? username, Guid courseOfferingId, CancellationToken cancellationToken = default);
}
