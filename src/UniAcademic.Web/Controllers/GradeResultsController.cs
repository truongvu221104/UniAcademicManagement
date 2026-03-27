using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Abstractions.GradeResults;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.GradeResults;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;
using UniAcademic.Web.Models.GradeResults;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class GradeResultsController : Controller
{
    private readonly IGradeResultService _gradeResultService;
    private readonly ICourseOfferingService _courseOfferingService;

    public GradeResultsController(
        IGradeResultService gradeResultService,
        ICourseOfferingService courseOfferingService)
    {
        _gradeResultService = gradeResultService;
        _courseOfferingService = courseOfferingService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.GradeResults.View)]
    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseOfferingId, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var results = await _gradeResultService.GetListAsync(new GetGradeResultsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedResults = PaginationHelper.Paginate(results, page, pageSize);

        ViewBag.CourseOfferingId = courseOfferingId;
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: true);
        ViewData["Pagination"] = pagedResults.Pagination;
        return View(pagedResults.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.GradeResults.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gradeResultService.GetByIdAsync(new GetGradeResultByIdQuery { Id = id }, cancellationToken);
            return View(result);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.GradeResults.Calculate)]
    [HttpGet]
    public async Task<IActionResult> Calculate(CancellationToken cancellationToken)
    {
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, null, includeEmpty: false);
        return View(new CalculateGradeResultsViewModel());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.GradeResults.Calculate)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(CalculateGradeResultsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }

        try
        {
            await _gradeResultService.CalculateAsync(new CalculateGradeResultsCommand
            {
                CourseOfferingId = model.CourseOfferingId,
                PassingScore = model.PassingScore
            }, cancellationToken);

            return RedirectToAction(nameof(Index), new { courseOfferingId = model.CourseOfferingId });
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildCourseOfferingOptionsAsync(CancellationToken cancellationToken, Guid? selectedCourseOfferingId, bool includeEmpty)
    {
        var courseOfferings = await _courseOfferingService.GetListAsync(new GetCourseOfferingsQuery(), cancellationToken);
        var options = courseOfferings
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.DisplayName}",
                Selected = selectedCourseOfferingId.HasValue && x.Id == selectedCourseOfferingId.Value
            })
            .ToList();

        if (includeEmpty)
        {
            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca lop hoc phan",
                Selected = !selectedCourseOfferingId.HasValue
            });
        }

        return options;
    }
}
