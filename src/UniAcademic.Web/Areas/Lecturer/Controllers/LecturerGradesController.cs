using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.LecturerPortal;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Grades;
using UniAcademic.Application.Models.LecturerPortal;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;
using UniAcademic.Web.Models.Grades;
using UniAcademic.Web.Models.LecturerPortal;

namespace UniAcademic.Web.Areas.Lecturer.Controllers;

[Area("Lecturer")]
[Authorize]
public sealed class LecturerGradesController : Controller
{
    private readonly ILecturerPortalService _lecturerPortalService;

    public LecturerGradesController(ILecturerPortalService lecturerPortalService)
    {
        _lecturerPortalService = lecturerPortalService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        var offerings = await _lecturerPortalService.GetMyTeachingOfferingsAsync(new GetMyTeachingOfferingsQuery
        {
            Keyword = keyword
        }, cancellationToken);
        var categories = await _lecturerPortalService.GetGradeCategoriesAsync(new GetLecturerGradeCategoriesQuery(), cancellationToken);
        var results = await _lecturerPortalService.GetGradeResultsAsync(new GetLecturerGradeResultsQuery(), cancellationToken);

        var categoryCounts = categories
            .GroupBy(x => x.CourseOfferingId)
            .ToDictionary(x => x.Key, x => x.Count());
        var resultCounts = results
            .GroupBy(x => x.CourseOfferingId)
            .ToDictionary(x => x.Key, x => x.Count());

        var overview = offerings.Select(x => new LecturerGradeOfferingOverviewItemViewModel
        {
            CourseOfferingId = x.Id,
            CourseOfferingCode = x.Code,
            DisplayName = x.DisplayName,
            CourseCode = x.CourseCode,
            CourseName = x.CourseName,
            SemesterName = x.SemesterName,
            Capacity = x.Capacity,
            CategoryCount = categoryCounts.GetValueOrDefault(x.Id, 0),
            ResultCount = resultCounts.GetValueOrDefault(x.Id, 0),
            IsGradebookEditable = resultCounts.GetValueOrDefault(x.Id, 0) == 0
        }).ToList();

        var pagedOverview = PaginationHelper.Paginate(overview, pageNumber, pageSize);
        ViewBag.Keyword = keyword;
        ViewData["Pagination"] = pagedOverview.Pagination;
        return View(pagedOverview.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.View)]
    [HttpGet]
    public async Task<IActionResult> Categories(Guid courseOfferingId, int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        var offerings = await _lecturerPortalService.GetMyTeachingOfferingsAsync(new GetMyTeachingOfferingsQuery(), cancellationToken);
        var selectedOffering = offerings.FirstOrDefault(x => x.Id == courseOfferingId);
        if (selectedOffering is null)
        {
            TempData["ErrorMessage"] = "The selected teaching offering could not be found.";
            return RedirectToAction(nameof(Index));
        }

        var categories = await _lecturerPortalService.GetGradeCategoriesAsync(new GetLecturerGradeCategoriesQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedCategories = PaginationHelper.Paginate(categories, pageNumber, pageSize);
        var isGradebookEditable = await _lecturerPortalService.IsGradebookEditableAsync(courseOfferingId, cancellationToken);

        ViewBag.CourseOfferingId = courseOfferingId;
        ViewBag.SelectedOffering = selectedOffering;
        ViewBag.IsGradebookEditable = isGradebookEditable;
        ViewData["Pagination"] = pagedCategories.Pagination;
        return View(pagedCategories.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _lecturerPortalService.GetGradeCategoryByIdAsync(id, cancellationToken);
            ViewBag.IsGradebookEditable = await _lecturerPortalService.IsGradebookEditableAsync(category.CourseOfferingId, cancellationToken);
            return View(category);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Create)]
    [HttpGet]
    public async Task<IActionResult> Create(Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        TempData["ErrorMessage"] = "Grade categories are managed by academic staff. Lecturers can update student scores only.";
        return courseOfferingId.HasValue
            ? RedirectToAction(nameof(Categories), new { courseOfferingId })
            : RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGradeCategoryViewModel model, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        TempData["ErrorMessage"] = "Grade categories are managed by academic staff. Lecturers can update student scores only.";
        return RedirectToAction(nameof(Categories), new { courseOfferingId = model.CourseOfferingId });
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Edit)]
    [HttpGet]
    public async Task<IActionResult> EditCategory(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _lecturerPortalService.GetGradeCategoryByIdAsync(id, cancellationToken);
            TempData["ErrorMessage"] = "Grade categories are managed by academic staff. Lecturers can update student scores only.";
            return RedirectToAction(nameof(Categories), new { courseOfferingId = category.CourseOfferingId });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(UpdateGradeCategoryViewModel model, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        TempData["ErrorMessage"] = "Grade categories are managed by academic staff. Lecturers can update student scores only.";
        return RedirectToAction(nameof(Categories), new { courseOfferingId = model.CourseOfferingId });
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Edit)]
    [HttpGet]
    public async Task<IActionResult> EditEntries(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _lecturerPortalService.GetGradeCategoryByIdAsync(id, cancellationToken);
            if (!await _lecturerPortalService.IsGradebookEditableAsync(category.CourseOfferingId, cancellationToken))
            {
                TempData["ErrorMessage"] = "This class grade book was already finalized. Student scores can no longer be changed from the lecturer portal.";
                return RedirectToAction(nameof(Categories), new { courseOfferingId = category.CourseOfferingId });
            }

            return View(Map(category));
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEntries(UpdateGradeEntriesViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _lecturerPortalService.UpdateGradeEntriesAsync(new UpdateGradeEntriesCommand
            {
                Id = model.Id,
                Entries = model.Entries.Select(x => new UpdateGradeEntryItemCommand
                {
                    RosterItemId = x.RosterItemId,
                    Score = x.Score,
                    Note = x.Note
                }).ToList()
            }, cancellationToken);

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private static UpdateGradeEntriesViewModel Map(GradeCategoryModel model)
    {
        return new UpdateGradeEntriesViewModel
        {
            Id = model.Id,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            Name = model.Name,
            Weight = model.Weight,
            MaxScore = model.MaxScore,
            Entries = model.Entries.Select(x => new UpdateGradeEntryItemViewModel
            {
                RosterItemId = x.RosterItemId,
                StudentCode = x.StudentCode,
                StudentFullName = x.StudentFullName,
                StudentClassName = x.StudentClassName,
                Score = x.Score,
                Note = x.Note
            }).ToList()
        };
    }
}
