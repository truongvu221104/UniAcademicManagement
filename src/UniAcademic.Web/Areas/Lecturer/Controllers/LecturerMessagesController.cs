using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Chat;
using UniAcademic.Web.Models.Chat;

namespace UniAcademic.Web.Areas.Lecturer.Controllers;

[Area("Lecturer")]
[Authorize]
public sealed class LecturerMessagesController : Controller
{
    private readonly ICourseChatService _courseChatService;

    public LecturerMessagesController(ICourseChatService courseChatService)
    {
        _courseChatService = courseChatService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        var rooms = await _courseChatService.GetMyRoomsAsync(cancellationToken);
        var selectedCourseOfferingId = ResolveSelectedCourseOfferingId(rooms, courseOfferingId);
        var conversation = selectedCourseOfferingId.HasValue
            ? await _courseChatService.GetConversationAsync(selectedCourseOfferingId.Value, cancellationToken)
            : null;

        return View(new CourseChatPageViewModel
        {
            RoomArea = "Lecturer",
            RoomController = "LecturerMessages",
            EmptyStateTitle = "Class Messages",
            EmptyStateDescription = "Choose one of your teaching classes to reply to students in context.",
            Rooms = rooms,
            Conversation = conversation
        });
    }

    private static Guid? ResolveSelectedCourseOfferingId(IReadOnlyCollection<UniAcademic.Application.Models.Chat.CourseChatRoomItemModel> rooms, Guid? requestedCourseOfferingId)
    {
        if (requestedCourseOfferingId.HasValue && rooms.Any(x => x.CourseOfferingId == requestedCourseOfferingId.Value))
        {
            return requestedCourseOfferingId.Value;
        }

        return rooms.FirstOrDefault()?.CourseOfferingId;
    }
}
