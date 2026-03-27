using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.StudentProfiles;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.StudentProfiles;
using UniAcademic.Web.Models.StudentPortal;

namespace UniAcademic.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize]
public sealed class StudentProfileController : Controller
{
    private readonly ICurrentStudentContext _currentStudentContext;
    private readonly IStudentProfileService _studentProfileService;

    public StudentProfileController(
        ICurrentStudentContext currentStudentContext,
        IStudentProfileService studentProfileService)
    {
        _currentStudentContext = currentStudentContext;
        _studentProfileService = studentProfileService;
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
            return RedirectToAction("Index", "StudentCourseOfferings", new { area = "Student" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(StudentOwnProfileViewModel model, CancellationToken cancellationToken)
    {
        StudentProfileModel profile;
        try
        {
            profile = await GetCurrentProfileAsync(cancellationToken);
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "StudentCourseOfferings", new { area = "Student" });
        }

        model.Id = profile.Id;
        model.StudentCode = profile.StudentCode;
        model.FullName = profile.FullName;
        model.StudentClassCode = profile.StudentClassCode;
        model.StudentClassName = profile.StudentClassName;
        model.Status = profile.Status;
        model.Note = profile.Note;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var updated = await _studentProfileService.UpdateAsync(new UpdateStudentProfileCommand
            {
                Id = profile.Id,
                Code = profile.StudentCode,
                FullName = profile.FullName,
                StudentClassId = profile.StudentClassId,
                Email = model.Email,
                Phone = model.Phone,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Address = model.Address,
                Status = profile.Status,
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

    private async Task<StudentProfileModel> GetCurrentProfileAsync(CancellationToken cancellationToken)
    {
        var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
        return await _studentProfileService.GetByIdAsync(new GetStudentProfileByIdQuery
        {
            Id = studentProfileId
        }, cancellationToken);
    }

    private static StudentOwnProfileViewModel Map(StudentProfileModel profile)
    {
        return new StudentOwnProfileViewModel
        {
            Id = profile.Id,
            StudentCode = profile.StudentCode,
            FullName = profile.FullName,
            StudentClassCode = profile.StudentClassCode,
            StudentClassName = profile.StudentClassName,
            Email = profile.Email,
            Phone = profile.Phone,
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            Address = profile.Address,
            Status = profile.Status,
            Note = profile.Note
        };
    }
}
