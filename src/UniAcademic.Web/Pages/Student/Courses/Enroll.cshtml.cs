using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Enrollments;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Enrollments;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;

namespace UniAcademic.Web.Pages.Student.Courses;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.Create)]
public sealed class EnrollModel : PageModel
{
    private readonly ICurrentStudentContext _currentStudentContext;
    private readonly IStudentPortalService _studentPortalService;
    private readonly IEnrollmentService _enrollmentService;

    public EnrollModel(
        ICurrentStudentContext currentStudentContext,
        IStudentPortalService studentPortalService,
        IEnrollmentService enrollmentService)
    {
        _currentStudentContext = currentStudentContext;
        _studentPortalService = studentPortalService;
        _enrollmentService = enrollmentService;
    }

    public StudentSelfEnrollCourseOfferingDetailModel? Offering { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadPageAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
            await _enrollmentService.EnrollAsync(new EnrollStudentCommand
            {
                StudentProfileId = studentProfileId,
                CourseOfferingId = Id,
                IsOverride = false
            }, cancellationToken);

            TempData["SuccessMessage"] = "Enrollment completed successfully.";
            return RedirectToPage("/Student/Enrollments/Index");
        }
        catch (AuthException ex) when (IsStudentContextFailure(ex))
        {
            return Forbid();
        }
        catch (AuthException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await LoadPageAsync(cancellationToken);
        }
    }

    public static string FormatSchedule(StudentSelfEnrollCourseOfferingDetailModel offering)
    {
        if (offering.DayOfWeek is < 1 or > 7 || offering.StartPeriod < 1 || offering.EndPeriod < offering.StartPeriod)
        {
            return "TBA";
        }

        return $"Day {offering.DayOfWeek}, Period {offering.StartPeriod}-{offering.EndPeriod}";
    }

    private async Task<IActionResult> LoadPageAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
            Offering = await _studentPortalService.GetSelfEnrollCourseOfferingByIdAsync(Id, cancellationToken);
            return Page();
        }
        catch (AuthException ex) when (IsStudentContextFailure(ex))
        {
            return Forbid();
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Student/Courses/Index");
        }
    }

    private static bool IsStudentContextFailure(AuthException ex)
        => string.Equals(ex.Message, "Current user is not authenticated.", StringComparison.Ordinal)
           || string.Equals(ex.Message, "Current user is not mapped to a student profile.", StringComparison.Ordinal)
           || string.Equals(ex.Message, "Current student profile was not found.", StringComparison.Ordinal);
}
