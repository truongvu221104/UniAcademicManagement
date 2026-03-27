using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Faculties;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Faculties;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Enums;
using UniAcademic.Web.Models.Faculties;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class FacultiesController : Controller
{
    private readonly IFacultyService _facultyService;

    public FacultiesController(IFacultyService facultyService)
    {
        _facultyService = facultyService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, FacultyStatus? status, int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        var faculties = await _facultyService.GetListAsync(new GetFacultiesQuery
        {
            Keyword = keyword,
            Status = status
        }, cancellationToken);
        var pagedFaculties = UniAcademic.Web.Helpers.PaginationHelper.Paginate(faculties, pageNumber, pageSize);

        ViewBag.Keyword = keyword;
        ViewBag.Status = status;
        ViewData["Pagination"] = pagedFaculties.Pagination;
        return View(pagedFaculties.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var faculty = await _facultyService.GetByIdAsync(new GetFacultyByIdQuery { Id = id }, cancellationToken);
            return View(faculty);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.Create)]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateFacultyViewModel());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFacultyViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _facultyService.CreateAsync(new CreateFacultyCommand
            {
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                Status = model.Status
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var faculty = await _facultyService.GetByIdAsync(new GetFacultyByIdQuery { Id = id }, cancellationToken);
            return View(new UpdateFacultyViewModel
            {
                Id = faculty.Id,
                Code = faculty.Code,
                Name = faculty.Name,
                Description = faculty.Description,
                Status = faculty.Status
            });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateFacultyViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _facultyService.UpdateAsync(new UpdateFacultyCommand
            {
                Id = model.Id,
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                Status = model.Status
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.Delete)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _facultyService.DeleteAsync(new DeleteFacultyCommand { Id = id }, cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
