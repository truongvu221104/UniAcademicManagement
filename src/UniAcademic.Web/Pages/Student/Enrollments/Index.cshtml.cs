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
using UniAcademic.Web.Helpers;

namespace UniAcademic.Web.Pages.Student.Enrollments;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.View)]
public sealed class IndexModel : PageModel
{
    private readonly ICurrentStudentContext _currentStudentContext;
    private readonly IStudentPortalService _studentPortalService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(
        ICurrentStudentContext currentStudentContext,
        IStudentPortalService studentPortalService,
        IEnrollmentService enrollmentService,
        IAuthorizationService authorizationService)
    {
        _currentStudentContext = currentStudentContext;
        _studentPortalService = studentPortalService;
        _enrollmentService = enrollmentService;
        _authorizationService = authorizationService;
    }

    public IReadOnlyCollection<StudentCurrentEnrollmentItemModel> Enrollments { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        try
        {
            await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
            var enrollments = await _studentPortalService.GetMyCurrentEnrollmentsAsync(cancellationToken);
            var pagedEnrollments = PaginationHelper.Paginate(enrollments, pageNumber, pageSize);
            Enrollments = pagedEnrollments.Items;
            ViewData["Pagination"] = pagedEnrollments.Pagination;
            return Page();
        }
        catch (AuthException ex) when (IsStudentContextFailure(ex))
        {
            return Forbid();
        }
    }

    public async Task<IActionResult> OnPostDropAsync(Guid enrollmentId, CancellationToken cancellationToken)
    {
        try
        {
            var authorizationResult = await _authorizationService.AuthorizeAsync(
                User,
                null,
                PermissionConstants.BuildPolicy(PermissionConstants.Enrollments.Delete));

            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
            await _studentPortalService.GetMyCurrentEnrollmentByIdAsync(enrollmentId, cancellationToken);
            await _enrollmentService.DropAsync(new DropEnrollmentCommand
            {
                Id = enrollmentId
            }, cancellationToken);

            TempData["SuccessMessage"] = "Enrollment dropped successfully.";
        }
        catch (AuthException ex) when (IsStudentContextFailure(ex))
        {
            return Forbid();
        }
        catch (AuthException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage();
    }

    public static string FormatSchedule(StudentCurrentEnrollmentItemModel enrollment)
    {
        if (enrollment.DayOfWeek is < 1 or > 7 || enrollment.StartPeriod < 1 || enrollment.EndPeriod < enrollment.StartPeriod)
        {
            return "TBA";
        }

        return $"Day {enrollment.DayOfWeek}, Period {enrollment.StartPeriod}-{enrollment.EndPeriod}";
    }

    private static bool IsStudentContextFailure(AuthException ex)
        => string.Equals(ex.Message, "Current user is not authenticated.", StringComparison.Ordinal)
           || string.Equals(ex.Message, "Current user is not mapped to a student profile.", StringComparison.Ordinal)
           || string.Equals(ex.Message, "Current student profile was not found.", StringComparison.Ordinal);
}
