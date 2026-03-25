using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniAcademic.Application.Abstractions.Faculties;
using UniAcademic.Application.Abstractions.LecturerProfiles;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Faculties;
using UniAcademic.Application.Models.LecturerProfiles;
using UniAcademic.Application.Security;
using UniAcademic.Web.Models.LecturerProfiles;

namespace UniAcademic.Web.Controllers;

[Authorize]
public sealed class LecturerProfilesController : Controller
{
    private readonly ILecturerProfileService _lecturerProfileService;
    private readonly IFacultyService _facultyService;

    public LecturerProfilesController(ILecturerProfileService lecturerProfileService, IFacultyService facultyService)
    {
        _lecturerProfileService = lecturerProfileService;
        _facultyService = facultyService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.View)]
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, Guid? facultyId, bool? isActive, CancellationToken cancellationToken)
    {
        var lecturerProfiles = await _lecturerProfileService.GetListAsync(new GetLecturerProfilesQuery
        {
            Keyword = keyword,
            FacultyId = facultyId,
            IsActive = isActive
        }, cancellationToken);

        ViewBag.Keyword = keyword;
        ViewBag.FacultyId = facultyId;
        ViewBag.IsActive = isActive;
        ViewBag.FacultyOptions = await BuildFacultyOptionsAsync(cancellationToken, facultyId, includeEmpty: true);

        return View(lecturerProfiles);
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.View)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var lecturerProfile = await _lecturerProfileService.GetByIdAsync(new GetLecturerProfileByIdQuery { Id = id }, cancellationToken);
            return View(lecturerProfile);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.Create)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadFacultyOptionsAsync(cancellationToken);
        return View(new CreateLecturerProfileViewModel());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.Create)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLecturerProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadFacultyOptionsAsync(cancellationToken, model.FacultyId);
            return View(model);
        }

        try
        {
            await _lecturerProfileService.CreateAsync(new CreateLecturerProfileCommand
            {
                Code = model.Code,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FacultyId = model.FacultyId,
                IsActive = model.IsActive,
                Note = model.Note
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

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.Edit)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var lecturerProfile = await _lecturerProfileService.GetByIdAsync(new GetLecturerProfileByIdQuery { Id = id }, cancellationToken);
            await LoadFacultyOptionsAsync(cancellationToken, lecturerProfile.FacultyId);

            return View(new UpdateLecturerProfileViewModel
            {
                Id = lecturerProfile.Id,
                Code = lecturerProfile.Code,
                FullName = lecturerProfile.FullName,
                Email = lecturerProfile.Email,
                PhoneNumber = lecturerProfile.PhoneNumber,
                FacultyId = lecturerProfile.FacultyId,
                IsActive = lecturerProfile.IsActive,
                Note = lecturerProfile.Note
            });
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateLecturerProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadFacultyOptionsAsync(cancellationToken, model.FacultyId);
            return View(model);
        }

        try
        {
            await _lecturerProfileService.UpdateAsync(new UpdateLecturerProfileCommand
            {
                Id = model.Id,
                Code = model.Code,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FacultyId = model.FacultyId,
                IsActive = model.IsActive,
                Note = model.Note
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

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.Delete)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _lecturerProfileService.DeleteAsync(new DeleteLecturerProfileCommand { Id = id }, cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFacultyOptionsAsync(CancellationToken cancellationToken, Guid? selectedFacultyId = null)
    {
        ViewBag.FacultyOptions = await BuildFacultyOptionsAsync(cancellationToken, selectedFacultyId, includeEmpty: false);
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
                Text = "Tat ca khoa",
                Selected = !selectedFacultyId.HasValue
            });
        }

        return options;
    }
}
