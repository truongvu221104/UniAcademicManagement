using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.LecturerProfiles;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.LecturerProfiles;
using UniAcademic.Web.Models.LecturerPortal;

namespace UniAcademic.Web.Areas.Lecturer.Controllers;

[Area("Lecturer")]
[Authorize]
public sealed class LecturerProfileController : Controller
{
    private readonly ICurrentLecturerContext _currentLecturerContext;
    private readonly ILecturerProfileService _lecturerProfileService;

    public LecturerProfileController(
        ICurrentLecturerContext currentLecturerContext,
        ILecturerProfileService lecturerProfileService)
    {
        _currentLecturerContext = currentLecturerContext;
        _lecturerProfileService = lecturerProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var profile = await GetCurrentProfileAsync(cancellationToken);
            return View(Map(profile));
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "LecturerOfferings", new { area = "Lecturer" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LecturerOwnProfileViewModel model, CancellationToken cancellationToken)
    {
        LecturerProfileModel profile;
        try
        {
            profile = await GetCurrentProfileAsync(cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "LecturerOfferings", new { area = "Lecturer" });
        }

        model.Id = profile.Id;
        model.Code = profile.Code;
        model.FullName = profile.FullName;
        model.FacultyCode = profile.FacultyCode;
        model.FacultyName = profile.FacultyName;
        model.IsActive = profile.IsActive;
        model.Note = profile.Note;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var updated = await _lecturerProfileService.UpdateAsync(new UpdateLecturerProfileCommand
            {
                Id = profile.Id,
                Code = profile.Code,
                FullName = profile.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FacultyId = profile.FacultyId,
                IsActive = profile.IsActive,
                Note = profile.Note
            }, cancellationToken);

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return View(Map(updated));
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private async Task<LecturerProfileModel> GetCurrentProfileAsync(CancellationToken cancellationToken)
    {
        var lecturerProfileId = await _currentLecturerContext.GetRequiredLecturerProfileIdAsync(cancellationToken);
        return await _lecturerProfileService.GetByIdAsync(new GetLecturerProfileByIdQuery
        {
            Id = lecturerProfileId
        }, cancellationToken);
    }

    private static LecturerOwnProfileViewModel Map(LecturerProfileModel profile)
    {
        return new LecturerOwnProfileViewModel
        {
            Id = profile.Id,
            Code = profile.Code,
            FullName = profile.FullName,
            FacultyCode = profile.FacultyCode,
            FacultyName = profile.FacultyName,
            Email = profile.Email,
            PhoneNumber = profile.PhoneNumber,
            IsActive = profile.IsActive,
            Note = profile.Note
        };
    }
}
