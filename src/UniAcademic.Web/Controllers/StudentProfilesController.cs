using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.StudentClasses;
using UniAcademic.Application.Abstractions.StudentProfiles;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.StudentClasses;
using UniAcademic.Application.Models.StudentProfiles;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Enums;
using UniAcademic.Web.Models.StudentProfiles;

namespace UniAcademic.Web.Controllers;

[Authorize(Roles = RoleConstants.AcademicManagement)]
public sealed class StudentProfilesController : Controller
{
    private readonly IStudentProfileService _studentProfileService;
    private readonly IStudentClassService _studentClassService;

    public StudentProfilesController(IStudentProfileService studentProfileService, IStudentClassService studentClassService)
    {
        _studentProfileService = studentProfileService;
        _studentClassService = studentClassService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, Guid? studentClassId, StudentGender? gender, StudentProfileStatus? status, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        var studentProfiles = await _studentProfileService.GetListAsync(new GetStudentProfilesQuery
        {
            Keyword = keyword,
            StudentClassId = studentClassId,
            Gender = gender,
            Status = status
        }, cancellationToken);
        var pagedStudentProfiles = UniAcademic.Web.Helpers.PaginationHelper.Paginate(studentProfiles, page, pageSize);

        ViewBag.Keyword = keyword;
        ViewBag.StudentClassId = studentClassId;
        ViewBag.Gender = gender;
        ViewBag.Status = status;
        ViewBag.StudentClassOptions = await BuildStudentClassOptionsAsync(cancellationToken, studentClassId, includeEmpty: true);

        ViewData["Pagination"] = pagedStudentProfiles.Pagination;
        return View(pagedStudentProfiles.Items);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var studentProfile = await _studentProfileService.GetByIdAsync(new GetStudentProfileByIdQuery { Id = id }, cancellationToken);
            return View(studentProfile);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.Create)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadStudentClassOptionsAsync(cancellationToken);
        return View(new CreateStudentProfileViewModel());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStudentProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadStudentClassOptionsAsync(cancellationToken, model.StudentClassId);
            return View(model);
        }

        try
        {
            await _studentProfileService.CreateAsync(new CreateStudentProfileCommand
            {
                Code = model.StudentCode,
                FullName = model.FullName,
                StudentClassId = model.StudentClassId,
                Email = model.Email,
                Phone = model.Phone,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Address = model.Address,
                Status = model.Status,
                Note = model.Note
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadStudentClassOptionsAsync(cancellationToken, model.StudentClassId);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var studentProfile = await _studentProfileService.GetByIdAsync(new GetStudentProfileByIdQuery { Id = id }, cancellationToken);
            await LoadStudentClassOptionsAsync(cancellationToken, studentProfile.StudentClassId);

            return View(new UpdateStudentProfileViewModel
            {
                Id = studentProfile.Id,
                StudentCode = studentProfile.StudentCode,
                FullName = studentProfile.FullName,
                StudentClassId = studentProfile.StudentClassId,
                Email = studentProfile.Email,
                Phone = studentProfile.Phone,
                DateOfBirth = studentProfile.DateOfBirth,
                Gender = studentProfile.Gender,
                Address = studentProfile.Address,
                Status = studentProfile.Status,
                Note = studentProfile.Note
            });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateStudentProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadStudentClassOptionsAsync(cancellationToken, model.StudentClassId);
            return View(model);
        }

        try
        {
            await _studentProfileService.UpdateAsync(new UpdateStudentProfileCommand
            {
                Id = model.Id,
                Code = model.StudentCode,
                FullName = model.FullName,
                StudentClassId = model.StudentClassId,
                Email = model.Email,
                Phone = model.Phone,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Address = model.Address,
                Status = model.Status,
                Note = model.Note
            }, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadStudentClassOptionsAsync(cancellationToken, model.StudentClassId);
            return View(model);
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.Delete)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _studentProfileService.DeleteAsync(new DeleteStudentProfileCommand { Id = id }, cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadStudentClassOptionsAsync(CancellationToken cancellationToken, Guid? selectedStudentClassId = null)
    {
        ViewBag.StudentClassOptions = await BuildStudentClassOptionsAsync(cancellationToken, selectedStudentClassId, includeEmpty: false);
    }

    private async Task<IReadOnlyCollection<SelectListItem>> BuildStudentClassOptionsAsync(CancellationToken cancellationToken, Guid? selectedStudentClassId, bool includeEmpty)
    {
        var studentClasses = await _studentClassService.GetListAsync(new GetStudentClassesQuery(), cancellationToken);
        var options = studentClasses
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Code} - {x.Name}",
                Selected = selectedStudentClassId.HasValue && x.Id == selectedStudentClassId.Value
            })
            .ToList();

        if (includeEmpty)
        {
            options.Insert(0, new SelectListItem
            {
                Value = string.Empty,
                Text = "Tat ca lop",
                Selected = !selectedStudentClassId.HasValue
            });
        }

        return options;
    }
}
