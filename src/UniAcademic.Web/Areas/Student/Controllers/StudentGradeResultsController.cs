using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;

namespace UniAcademic.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize]
public sealed class StudentGradeResultsController : Controller
{
    private readonly IStudentPortalService _studentPortalService;

    public StudentGradeResultsController(IStudentPortalService studentPortalService)
    {
        _studentPortalService = studentPortalService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.GradeResults.View)]
    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseOfferingId, int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        var offerings = await _studentPortalService.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery(), cancellationToken);
        var results = await _studentPortalService.GetMyGradeResultsAsync(new GetMyGradeResultsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedResults = PaginationHelper.Paginate(results, pageNumber, pageSize);

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

        ViewData["Pagination"] = pagedResults.Pagination;
        return View(pagedResults.Items);
    }
}
