using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.LecturerPortal;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Grades;
using UniAcademic.Application.Models.LecturerPortal;
using UniAcademic.Application.Security;
using UniAcademic.Web.Helpers;
using UniAcademic.Web.Models.Grades;

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
    public async Task<IActionResult> Index(Guid? courseOfferingId, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        await LoadOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: true);
        var categories = await _lecturerPortalService.GetGradeCategoriesAsync(new GetLecturerGradeCategoriesQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);
        var pagedCategories = PaginationHelper.Paginate(categories, page, pageSize);
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
        await LoadOfferingOptionsAsync(cancellationToken, courseOfferingId, includeEmpty: false);
        return View(new CreateGradeCategoryViewModel
        {
            CourseOfferingId = courseOfferingId ?? Guid.Empty
        });
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGradeCategoryViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }

        try
        {
            var result = await _lecturerPortalService.CreateGradeCategoryAsync(new CreateGradeCategoryCommand
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
            await LoadOfferingOptionsAsync(cancellationToken, model.CourseOfferingId, includeEmpty: false);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Edit)]
    [HttpGet]
    public async Task<IActionResult> EditCategory(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _lecturerPortalService.GetGradeCategoryByIdAsync(id, cancellationToken);
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
            await _lecturerPortalService.UpdateGradeCategoryAsync(new UpdateGradeCategoryCommand
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
            var category = await _lecturerPortalService.GetGradeCategoryByIdAsync(id, cancellationToken);
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
