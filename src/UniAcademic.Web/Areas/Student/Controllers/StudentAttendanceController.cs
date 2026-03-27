using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Enums;
using UniAcademic.Web.Models.StudentPortal;

namespace UniAcademic.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize]
public sealed class StudentAttendanceController : Controller
{
    private readonly IStudentPortalService _studentPortalService;

    public StudentAttendanceController(IStudentPortalService studentPortalService)
    {
        _studentPortalService = studentPortalService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, CancellationToken cancellationToken)
    {
        var offerings = await _studentPortalService.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery
        {
            Keyword = keyword
        }, cancellationToken);
        var attendance = await _studentPortalService.GetMyAttendanceAsync(new GetMyAttendanceQuery(), cancellationToken);
        var groupedAttendance = attendance
            .GroupBy(x => x.CourseOfferingId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var overview = offerings
            .Select(x =>
            {
                groupedAttendance.TryGetValue(x.Id, out var sessions);
                sessions ??= new List<StudentAttendanceItemModel>();

                return new StudentAttendanceOverviewItemViewModel
                {
                    CourseOfferingId = x.Id,
                    CourseOfferingCode = x.Code,
                    CourseName = x.CourseName,
                    SemesterName = x.SemesterName,
                    SessionCount = sessions.Count,
                    PresentCount = sessions.Count(y => y.Status == AttendanceStatus.Present),
                    LateCount = sessions.Count(y => y.Status == AttendanceStatus.Late),
                    AbsentCount = sessions.Count(y => y.Status == AttendanceStatus.Absent),
                    ExcusedCount = sessions.Count(y => y.Status == AttendanceStatus.Excused)
                };
            })
            .ToList();

        ViewBag.Keyword = keyword;
        return View(overview);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var offerings = await _studentPortalService.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery(), cancellationToken);
        var offering = offerings.FirstOrDefault(x => x.Id == courseOfferingId);
        if (offering is null)
        {
            TempData["ErrorMessage"] = "Course offering was not found for the current student.";
            return RedirectToAction(nameof(Index));
        }

        var attendance = await _studentPortalService.GetMyAttendanceAsync(new GetMyAttendanceQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);

        var model = new StudentAttendanceDetailsViewModel
        {
            CourseOfferingId = offering.Id,
            CourseOfferingCode = offering.Code,
            CourseName = offering.CourseName,
            SemesterName = offering.SemesterName,
            Sessions = attendance
        };

        return View(model);
    }
}
