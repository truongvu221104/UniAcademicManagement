using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Enrollments;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Common;
using UniAcademic.Application.Models.Enrollments;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;
using System.Security.Claims;

namespace UniAcademic.Web.Pages.Student.Courses;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.Create)]
public sealed class EnrollModel : PageModel
{
    private readonly ICurrentStudentContext _currentStudentContext;
    private readonly IStudentPortalService _studentPortalService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IEmailSender _emailSender;
    private readonly IAppDbContext _dbContext;

    public EnrollModel(
        ICurrentStudentContext currentStudentContext,
        IStudentPortalService studentPortalService,
        IEnrollmentService enrollmentService,
        IEmailSender emailSender,
        IAppDbContext dbContext)
    {
        _currentStudentContext = currentStudentContext;
        _studentPortalService = studentPortalService;
        _enrollmentService = enrollmentService;
        _emailSender = emailSender;
        _dbContext = dbContext;
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
            await TrySendEnrollmentEmailAsync(cancellationToken);
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

    private async Task TrySendEnrollmentEmailAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return;
        }

        var email = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == parsedUserId)
            .Select(x => x.StudentProfile != null && x.StudentProfile.Email != null
                ? x.StudentProfile.Email
                : x.Email)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        StudentSelfEnrollCourseOfferingDetailModel? offering;
        try
        {
            offering = await _studentPortalService.GetSelfEnrollCourseOfferingByIdAsync(Id, cancellationToken);
        }
        catch
        {
            return;
        }

        try
        {
            var studentName = User.Identity?.Name ?? "Student";
            var plainTextBody =
$@"Hello {studentName},

Your enrollment request was completed successfully.

Course offering: {offering.Code}
Course: {offering.CourseCode} - {offering.CourseName}
Semester: {offering.SemesterName}

You can review your current enrollments in the student portal.
";

            var htmlBody =
$"""
<p>Hello {System.Net.WebUtility.HtmlEncode(studentName)},</p>
<p>Your enrollment request was completed successfully.</p>
<p>
<strong>Course offering:</strong> {System.Net.WebUtility.HtmlEncode(offering.Code)}<br />
<strong>Course:</strong> {System.Net.WebUtility.HtmlEncode(offering.CourseCode)} - {System.Net.WebUtility.HtmlEncode(offering.CourseName)}<br />
<strong>Semester:</strong> {System.Net.WebUtility.HtmlEncode(offering.SemesterName)}
</p>
<p>You can review your current enrollments in the student portal.</p>
""";

            await _emailSender.SendAsync(new EmailMessage
            {
                ToEmail = email,
                ToName = studentName,
                Subject = $"Enrollment confirmed: {offering.Code}",
                PlainTextBody = plainTextBody,
                HtmlBody = htmlBody
            }, cancellationToken);
        }
        catch
        {
            TempData["SuccessMessage"] = "Enrollment completed successfully, but the confirmation email could not be sent.";
        }
    }
}
