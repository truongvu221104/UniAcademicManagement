using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.Faculties;
using UniAcademic.Application.Abstractions.StudentClasses;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Faculties;
using UniAcademic.Application.Models.StudentClasses;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Enums;
using UniAcademic.Web.Models.StudentClasses;

namespace UniAcademic.Web.Controllers;

[Authorize]
public sealed class StudentClassesController : Controller
{
    private readonly IStudentClassService _studentClassService;
    private readonly IFacultyService _facultyService;

    public StudentClassesController(IStudentClassService studentClassService, IFacultyService facultyService)
    {
        _studentClassService = studentClassService;
        _facultyService = facultyService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, Guid? facultyId, int? intakeYear, StudentClassStatus? status, CancellationToken cancellationToken)
    {
        var studentClasses = await _studentClassService.GetListAsync(new GetStudentClassesQuery
        {
            Keyword = keyword,
            FacultyId = facultyId,
            IntakeYear = intakeYear,
            Status = status
        }, cancellationToken);

        ViewBag.Keyword = keyword;
        ViewBag.FacultyId = facultyId;
        ViewBag.IntakeYear = intakeYear;
        ViewBag.Status = status;
        ViewBag.FacultyOptions = await BuildFacultyOptionsAsync(cancellationToken, facultyId);

        return View(studentClasses);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var studentClass = await _studentClassService.GetByIdAsync(new GetStudentClassByIdQuery { Id = id }, cancellationToken);
            return View(studentClass);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.Create)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadFacultyOptionsAsync(cancellationToken);
        return View(new CreateStudentClassViewModel
        {
            IntakeYear = DateTime.UtcNow.Year
        });
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStudentClassViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadFacultyOptionsAsync(cancellationToken, model.FacultyId);
            return View(model);
        }

        try
        {
            await _studentClassService.CreateAsync(new CreateStudentClassCommand
            {
                Code = model.Code,
                Name = model.Name,
                FacultyId = model.FacultyId,
                IntakeYear = model.IntakeYear,
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

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var studentClass = await _studentClassService.GetByIdAsync(new GetStudentClassByIdQuery { Id = id }, cancellationToken);
            await LoadFacultyOptionsAsync(cancellationToken, studentClass.FacultyId);

            return View(new UpdateStudentClassViewModel
            {
                Id = studentClass.Id,
                Code = studentClass.Code,
                Name = studentClass.Name,
                FacultyId = studentClass.FacultyId,
                IntakeYear = studentClass.IntakeYear,
                Status = studentClass.Status,
                Description = studentClass.Description
            });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateStudentClassViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadFacultyOptionsAsync(cancellationToken, model.FacultyId);
            return View(model);
        }

        try
        {
            await _studentClassService.UpdateAsync(new UpdateStudentClassCommand
            {
                Id = model.Id,
                Code = model.Code,
                Name = model.Name,
                FacultyId = model.FacultyId,
                IntakeYear = model.IntakeYear,
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

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.Delete)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _studentClassService.DeleteAsync(new DeleteStudentClassCommand { Id = id }, cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFacultyOptionsAsync(CancellationToken cancellationToken, Guid? selectedFacultyId = null)
    {
        ViewBag.FacultyOptions = await BuildFacultyOptionsAsync(cancellationToken, selectedFacultyId);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildFacultyOptionsAsync(CancellationToken cancellationToken, Guid? selectedFacultyId = null)
    {
        var faculties = await _facultyService.GetListAsync(new GetFacultiesQuery(), cancellationToken);
        return faculties
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.Name}",
                Selected = selectedFacultyId.HasValue && x.Id == selectedFacultyId.Value
            })
            .ToList();
    }
}
