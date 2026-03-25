using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;

namespace UniAcademic.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize]
public sealed class StudentCourseOfferingsController : Controller
{
    private readonly IStudentPortalService _studentPortalService;

    public StudentCourseOfferingsController(IStudentPortalService studentPortalService)
    {
        _studentPortalService = studentPortalService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, CancellationToken cancellationToken)
    {
        var offerings = await _studentPortalService.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery
        {
            Keyword = keyword
        }, cancellationToken);

        ViewBag.Keyword = keyword;
        return View(offerings);
    }
}
