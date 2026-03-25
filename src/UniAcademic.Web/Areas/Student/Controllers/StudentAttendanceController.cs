using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;

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
    public async Task<IActionResult> Index(Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        var offerings = await _studentPortalService.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery(), cancellationToken);
        var attendance = await _studentPortalService.GetMyAttendanceAsync(new GetMyAttendanceQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);

        ViewBag.CourseOfferingId = courseOfferingId;
        ViewBag.CourseOfferingOptions = offerings
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.DisplayName}",
                Selected = courseOfferingId.HasValue && x.Id == courseOfferingId.Value
            })
            .Prepend(new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca hoc phan",
                Selected = !courseOfferingId.HasValue
            })
            .ToList();

        return View(attendance);
    }
}
