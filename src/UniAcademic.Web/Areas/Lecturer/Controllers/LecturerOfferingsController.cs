using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.LecturerPortal;
using UniAcademic.Application.Models.LecturerPortal;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;

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
    public async Task<IActionResult> Index(string? keyword, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var offerings = await _lecturerPortalService.GetMyTeachingOfferingsAsync(new GetMyTeachingOfferingsQuery
        {
            Keyword = keyword
        }, cancellationToken);
        var pagedOfferings = PaginationHelper.Paginate(offerings, page, pageSize);

        ViewBag.Keyword = keyword;
        ViewData["Pagination"] = pagedOfferings.Pagination;
        return View(pagedOfferings.Items);
    }
}
