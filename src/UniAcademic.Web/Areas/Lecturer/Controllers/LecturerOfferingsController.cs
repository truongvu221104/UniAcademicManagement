using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.LecturerPortal;
using UniAcademic.Application.Models.LecturerPortal;
using UniAcademic.Application.Security;

namespace UniAcademic.Web.Areas.Lecturer.Controllers;

[Area("Lecturer")]
[Authorize]
public sealed class LecturerOfferingsController : Controller
{
    private readonly ILecturerPortalService _lecturerPortalService;

    public LecturerOfferingsController(ILecturerPortalService lecturerPortalService)
    {
        _lecturerPortalService = lecturerPortalService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, CancellationToken cancellationToken)
    {
        var offerings = await _lecturerPortalService.GetMyTeachingOfferingsAsync(new GetMyTeachingOfferingsQuery
        {
            Keyword = keyword
        }, cancellationToken);

        ViewBag.Keyword = keyword;
        return View(offerings);
    }
}
