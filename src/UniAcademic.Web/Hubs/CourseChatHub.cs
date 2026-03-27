using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using UniAcademic.Application.Abstractions.Chat;
using UniAcademic.Application.Models.Chat;

namespace UniAcademic.Web.Hubs;

[Authorize]
public sealed class CourseChatHub : Hub
{
    private readonly ICourseChatService _courseChatService;

    public CourseChatHub(ICourseChatService courseChatService)
    {
        _courseChatService = courseChatService;
    }

    public async Task JoinCourseOffering(Guid courseOfferingId)
    {
        var (userId, username) = GetCurrentUserIdentity();

        if (!await _courseChatService.CanAccessCourseOfferingAsync(userId, username, courseOfferingId))
        {
            throw new HubException("You do not have access to this class conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(courseOfferingId));
    }

    public Task LeaveCourseOffering(Guid courseOfferingId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(courseOfferingId));
    }

    public async Task SendMessage(Guid courseOfferingId, string messageText)
    {
        var (userId, username) = GetCurrentUserIdentity();

        var message = await _courseChatService.SendMessageAsync(userId, username, new SendCourseChatMessageCommand
        {
            CourseOfferingId = courseOfferingId,
            MessageText = messageText
        });

        await Clients.Group(GetGroupName(courseOfferingId)).SendAsync("messageReceived", new
        {
            id = message.Id,
            courseOfferingId = message.CourseOfferingId,
            senderUserId = message.SenderUserId,
            senderDisplayName = message.SenderDisplayName,
            senderRole = message.SenderRole,
            messageText = message.MessageText,
            sentAtUtc = message.SentAtUtc
        });
    }

    public static string GetGroupName(Guid courseOfferingId) => $"course-offering:{courseOfferingId:N}";

    private (Guid? UserId, string? Username) GetCurrentUserIdentity()
    {
        var userIdValue = Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = Context.User?.Identity?.Name ?? Context.User?.FindFirstValue(ClaimTypes.Name);
        return (Guid.TryParse(userIdValue, out var userId) ? userId : null, username);
    }
}
