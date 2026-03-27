using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.LecturerPortal;
using UniAcademic.Application.Models.LecturerPortal;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;

namespace UniAcademic.Web.Areas.Lecturer.Controllers;

[Area("Lecturer")]
[Authorize]
public sealed class LecturerGradeResultsController : Controller
{
    private readonly ILecturerPortalService _lecturerPortalService;

    public LecturerGradeResultsController(ILecturerPortalService lecturerPortalService)
    {
        _lecturerPortalService = lecturerPortalService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.GradeResults.View)]
    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseOfferingId, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        await LoadOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: true);
        var results = await _lecturerPortalService.GetGradeResultsAsync(new GetLecturerGradeResultsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedResults = PaginationHelper.Paginate(results, page, pageSize);
        ViewData["Pagination"] = pagedResults.Pagination;
        return View(pagedResults.Items);
    }

    private async Task LoadOfferingOptionsAsync(CancellationToken cancellationToken, Guid? selectedCourseOfferingId, bool includeEmpty)
    {
        var offerings = await _lecturerPortalService.GetMyTeachingOfferingsAsync(new GetMyTeachingOfferingsQuery(), cancellationToken);
        var options = offerings.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text = $"{x.Code} - {x.DisplayName}",
            Selected = selectedCourseOfferingId.HasValue && x.Id == selectedCourseOfferingId.Value
        }).ToList();

        if (includeEmpty)
        {
            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca hoc phan",
                Selected = !selectedCourseOfferingId.HasValue
            });
        }

        ViewBag.CourseOfferingOptions = options;
    }
}
