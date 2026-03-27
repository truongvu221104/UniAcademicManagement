using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Semesters;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Semesters;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Enums;
using UniAcademic.Web.Models.Semesters;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class SemestersController : Controller
{
    private readonly ISemesterService _semesterService;

    public SemestersController(ISemesterService semesterService)
    {
        _semesterService = semesterService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, string? academicYear, int? termNo, SemesterStatus? status, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var semesters = await _semesterService.GetListAsync(new GetSemestersQuery
        {
            Keyword = keyword,
            AcademicYear = academicYear,
            TermNo = termNo,
            Status = status
        }, cancellationToken);
        var pagedSemesters = UniAcademic.Web.Helpers.PaginationHelper.Paginate(semesters, page, pageSize);

        ViewBag.Keyword = keyword;
        ViewBag.AcademicYear = academicYear;
        ViewBag.TermNo = termNo;
        ViewBag.Status = status;

        ViewData["Pagination"] = pagedSemesters.Pagination;
        return View(pagedSemesters.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var semester = await _semesterService.GetByIdAsync(new GetSemesterByIdQuery { Id = id }, cancellationToken);
            return View(semester);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.Create)]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateSemesterViewModel
        {
            TermNo = 1,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today
        });
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSemesterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _semesterService.CreateAsync(new CreateSemesterCommand
            {
                Code = model.Code,
                Name = model.Name,
                AcademicYear = model.AcademicYear,
                TermNo = model.TermNo,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Status = model.Status,
                Description = model.Description
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var semester = await _semesterService.GetByIdAsync(new GetSemesterByIdQuery { Id = id }, cancellationToken);
            return View(new UpdateSemesterViewModel
            {
                Id = semester.Id,
                Code = semester.Code,
                Name = semester.Name,
                AcademicYear = semester.AcademicYear,
                TermNo = semester.TermNo,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                Status = semester.Status,
                Description = semester.Description
            });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateSemesterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _semesterService.UpdateAsync(new UpdateSemesterCommand
            {
                Id = model.Id,
                Code = model.Code,
                Name = model.Name,
                AcademicYear = model.AcademicYear,
                TermNo = model.TermNo,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Status = model.Status,
                Description = model.Description
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.Delete)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _semesterService.DeleteAsync(new DeleteSemesterCommand { Id = id }, cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
