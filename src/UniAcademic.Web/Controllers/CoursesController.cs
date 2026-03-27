using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.Courses;
using UniAcademic.Application.Abstractions.Faculties;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Courses;
using UniAcademic.Application.Models.Faculties;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Enums;
using UniAcademic.Web.Models.Courses;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class CoursesController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IFacultyService _facultyService;

    public CoursesController(ICourseService courseService, IFacultyService facultyService)
    {
        _courseService = courseService;
        _facultyService = facultyService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, Guid? facultyId, CourseStatus? status, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var courses = await _courseService.GetListAsync(new GetCoursesQuery
        {
            Keyword = keyword,
            FacultyId = facultyId,
            Status = status
        }, cancellationToken);
        var pagedCourses = UniAcademic.Web.Helpers.PaginationHelper.Paginate(courses, page, pageSize);

        ViewBag.Keyword = keyword;
        ViewBag.FacultyId = facultyId;
        ViewBag.Status = status;
        ViewBag.FacultyOptions = await BuildFacultyOptionsAsync(cancellationToken, facultyId, includeEmpty: true);

        ViewData["Pagination"] = pagedCourses.Pagination;
        return View(pagedCourses.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var course = await _courseService.GetByIdAsync(new GetCourseByIdQuery { Id = id }, cancellationToken);
            return View(course);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.Create)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadFacultyOptionsAsync(cancellationToken);
        return View(new CreateCourseViewModel
        {
            Credits = 3
        });
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCourseViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadFacultyOptionsAsync(cancellationToken, model.FacultyId);
            return View(model);
        }

        try
        {
            await _courseService.CreateAsync(new CreateCourseCommand
            {
                Code = model.Code,
                Name = model.Name,
                Credits = model.Credits,
                FacultyId = model.FacultyId,
                Status = model.Status,
                Description = model.Description
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadFacultyOptionsAsync(cancellationToken, model.FacultyId);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var course = await _courseService.GetByIdAsync(new GetCourseByIdQuery { Id = id }, cancellationToken);
            await LoadFacultyOptionsAsync(cancellationToken, course.FacultyId);

            return View(new UpdateCourseViewModel
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name,
                Credits = course.Credits,
                FacultyId = course.FacultyId,
                Status = course.Status,
                Description = course.Description
            });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateCourseViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadFacultyOptionsAsync(cancellationToken, model.FacultyId);
            return View(model);
        }

        try
        {
            await _courseService.UpdateAsync(new UpdateCourseCommand
            {
                Id = model.Id,
                Code = model.Code,
                Name = model.Name,
                Credits = model.Credits,
                FacultyId = model.FacultyId,
                Status = model.Status,
                Description = model.Description
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadFacultyOptionsAsync(cancellationToken, model.FacultyId);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.Delete)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _courseService.DeleteAsync(new DeleteCourseCommand { Id = id }, cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFacultyOptionsAsync(CancellationToken cancellationToken, Guid? selectedFacultyId = null)
    {
        ViewBag.FacultyOptions = await BuildFacultyOptionsAsync(cancellationToken, selectedFacultyId, includeEmpty: true);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildFacultyOptionsAsync(CancellationToken cancellationToken, Guid? selectedFacultyId, bool includeEmpty)
    {
        var faculties = await _facultyService.GetListAsync(new GetFacultiesQuery(), cancellationToken);
        var options = faculties
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.Name}",
                Selected = selectedFacultyId.HasValue && x.Id == selectedFacultyId.Value
            })
            .ToList();

        if (includeEmpty)
        {
            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Khong gan khoa",
                Selected = !selectedFacultyId.HasValue
            });
        }

        return options;
    }
}
