using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Abstractions.Grades;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.Grades;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;
using UniAcademic.Web.Models.Grades;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class GradesController : Controller
{
    private readonly IGradeService _gradeService;
    private readonly ICourseOfferingService _courseOfferingService;

    public GradesController(
        IGradeService gradeService,
        ICourseOfferingService courseOfferingService)
    {
        _gradeService = gradeService;
        _courseOfferingService = courseOfferingService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.View)]
    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseOfferingId, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var categories = await _gradeService.GetListAsync(new GetGradeCategoriesQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedCategories = PaginationHelper.Paginate(categories, page, pageSize);

        ViewBag.CourseOfferingId = courseOfferingId;
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: true);
        ViewData["Pagination"] = pagedCategories.Pagination;
        return View(pagedCategories.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _gradeService.GetByIdAsync(new GetGradeCategoryByIdQuery { Id = id }, cancellationToken);
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
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, null, includeEmpty: false);
        return View(new CreateGradeCategoryViewModel());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGradeCategoryViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }

        try
        {
            var result = await _gradeService.CreateCategoryAsync(new CreateGradeCategoryCommand
            {
                CourseOfferingId = model.CourseOfferingId,
                Name = model.Name,
                Weight = model.Weight,
                MaxScore = model.MaxScore,
                OrderIndex = model.OrderIndex,
                IsActive = model.IsActive
            }, cancellationToken);

            return RedirectToAction(nameof(EditEntries), new { id = result.Id });
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.CourseOfferingOptions = await BuildCourseOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Edit)]
    [HttpGet]
    public async Task<IActionResult> EditCategory(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _gradeService.GetByIdAsync(new GetGradeCategoryByIdQuery { Id = id }, cancellationToken);
            return View(new UpdateGradeCategoryViewModel
            {
                Id = category.Id,
                CourseOfferingId = category.CourseOfferingId,
                CourseOfferingCode = category.CourseOfferingCode,
                CourseName = category.CourseName,
                SemesterName = category.SemesterName,
                Name = category.Name,
                Weight = category.Weight,
                MaxScore = category.MaxScore,
                OrderIndex = category.OrderIndex,
                IsActive = category.IsActive
            });
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
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _gradeService.UpdateCategoryAsync(new UpdateGradeCategoryCommand
            {
                Id = model.Id,
                Name = model.Name,
                Weight = model.Weight,
                MaxScore = model.MaxScore,
                OrderIndex = model.OrderIndex,
                IsActive = model.IsActive
            }, cancellationToken);

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Edit)]
    [HttpGet]
    public async Task<IActionResult> EditEntries(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _gradeService.GetByIdAsync(new GetGradeCategoryByIdQuery { Id = id }, cancellationToken);
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
            await _gradeService.UpdateEntriesAsync(new UpdateGradeEntriesCommand
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
