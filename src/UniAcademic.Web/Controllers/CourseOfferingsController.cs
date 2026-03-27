using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Abstractions.Courses;
using UniAcademic.Application.Abstractions.Semesters;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Models.Courses;
using UniAcademic.Application.Models.Semesters;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Enums;
using UniAcademic.Web.Models.CourseOfferings;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class CourseOfferingsController : Controller
{
    private readonly ICourseOfferingService _courseOfferingService;
    private readonly ICourseService _courseService;
    private readonly ISemesterService _semesterService;

    public CourseOfferingsController(
        ICourseOfferingService courseOfferingService,
        ICourseService courseService,
        ISemesterService semesterService)
    {
        _courseOfferingService = courseOfferingService;
        _courseService = courseService;
        _semesterService = semesterService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, Guid? courseId, Guid? semesterId, CourseOfferingStatus? status, int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        var offerings = await _courseOfferingService.GetListAsync(new GetCourseOfferingsQuery
        {
            Keyword = keyword,
            CourseId = courseId,
            SemesterId = semesterId,
            Status = status
        }, cancellationToken);
        var pagedOfferings = UniAcademic.Web.Helpers.PaginationHelper.Paginate(offerings, pageNumber, pageSize);

        ViewBag.Keyword = keyword;
        ViewBag.CourseId = courseId;
        ViewBag.SemesterId = semesterId;
        ViewBag.Status = status;
        ViewBag.CourseOptions = await BuildCourseOptionsAsync(cancellationToken, courseId, includeEmpty: true);
        ViewBag.SemesterOptions = await BuildSemesterOptionsAsync(cancellationToken, semesterId, includeEmpty: true);

        ViewData["Pagination"] = pagedOfferings.Pagination;
        return View(pagedOfferings.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var offering = await _courseOfferingService.GetByIdAsync(new GetCourseOfferingByIdQuery { Id = id }, cancellationToken);
            return View(offering);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.Create)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadReferenceOptionsAsync(cancellationToken);
        return View(new CreateCourseOfferingViewModel
        {
            Capacity = 50
        });
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCourseOfferingViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadReferenceOptionsAsync(cancellationToken, model.CourseId, model.SemesterId);
            return View(model);
        }

        try
        {
            await _courseOfferingService.CreateAsync(new CreateCourseOfferingCommand
            {
                Code = model.Code,
                CourseId = model.CourseId,
                SemesterId = model.SemesterId,
                DisplayName = model.DisplayName,
                Capacity = model.Capacity,
                Status = model.Status,
                Description = model.Description
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadReferenceOptionsAsync(cancellationToken, model.CourseId, model.SemesterId);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var offering = await _courseOfferingService.GetByIdAsync(new GetCourseOfferingByIdQuery { Id = id }, cancellationToken);
            await LoadReferenceOptionsAsync(cancellationToken, offering.CourseId, offering.SemesterId);

            return View(new UpdateCourseOfferingViewModel
            {
                Id = offering.Id,
                Code = offering.Code,
                CourseId = offering.CourseId,
                SemesterId = offering.SemesterId,
                DisplayName = offering.DisplayName,
                Capacity = offering.Capacity,
                Status = offering.Status,
                Description = offering.Description
            });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateCourseOfferingViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadReferenceOptionsAsync(cancellationToken, model.CourseId, model.SemesterId);
            return View(model);
        }

        try
        {
            await _courseOfferingService.UpdateAsync(new UpdateCourseOfferingCommand
            {
                Id = model.Id,
                Code = model.Code,
                CourseId = model.CourseId,
                SemesterId = model.SemesterId,
                DisplayName = model.DisplayName,
                Capacity = model.Capacity,
                Status = model.Status,
                Description = model.Description
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadReferenceOptionsAsync(cancellationToken, model.CourseId, model.SemesterId);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.Delete)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _courseOfferingService.DeleteAsync(new DeleteCourseOfferingCommand { Id = id }, cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadReferenceOptionsAsync(CancellationToken cancellationToken, Guid? selectedCourseId = null, Guid? selectedSemesterId = null)
    {
        ViewBag.CourseOptions = await BuildCourseOptionsAsync(cancellationToken, selectedCourseId, includeEmpty: false);
        ViewBag.SemesterOptions = await BuildSemesterOptionsAsync(cancellationToken, selectedSemesterId, includeEmpty: false);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildCourseOptionsAsync(CancellationToken cancellationToken, Guid? selectedCourseId, bool includeEmpty)
    {
        var courses = await _courseService.GetListAsync(new GetCoursesQuery(), cancellationToken);
        var options = courses
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.Name}",
                Selected = selectedCourseId.HasValue && x.Id == selectedCourseId.Value
            })
            .ToList();

        if (includeEmpty)
        {
            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca hoc phan",
                Selected = !selectedCourseId.HasValue
            });
        }

        return options;
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildSemesterOptionsAsync(CancellationToken cancellationToken, Guid? selectedSemesterId, bool includeEmpty)
    {
        var semesters = await _semesterService.GetListAsync(new GetSemestersQuery(), cancellationToken);
        var options = semesters
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.Name}",
                Selected = selectedSemesterId.HasValue && x.Id == selectedSemesterId.Value
            })
            .ToList();

        if (includeEmpty)
        {
            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca hoc ky",
                Selected = !selectedSemesterId.HasValue
            });
        }

        return options;
    }
}
