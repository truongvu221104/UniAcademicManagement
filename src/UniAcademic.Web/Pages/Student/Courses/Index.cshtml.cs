using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;

namespace UniAcademic.Web.Pages.Student.Courses;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.View)]
public sealed class IndexModel : PageModel
{
    private readonly ICurrentStudentContext _currentStudentContext;
    private readonly IStudentPortalService _studentPortalService;

    public IndexModel(
        ICurrentStudentContext currentStudentContext,
        IStudentPortalService studentPortalService)
    {
        _currentStudentContext = currentStudentContext;
        _studentPortalService = studentPortalService;
    }

    public IReadOnlyCollection<StudentSelfEnrollCourseOfferingItemModel> Offerings { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
            Offerings = await _studentPortalService.GetSelfEnrollCourseOfferingsAsync(new GetSelfEnrollCourseOfferingsQuery
            {
                Keyword = Keyword
            }, cancellationToken);

            return Page();
        }
        catch (AuthException ex) when (IsStudentContextFailure(ex))
        {
            return Forbid();
        }
    }

    public static string FormatSchedule(StudentSelfEnrollCourseOfferingItemModel offering)
    {
        if (offering.DayOfWeek is < 1 or > 7 || offering.StartPeriod < 1 || offering.EndPeriod < offering.StartPeriod)
        {
            return "TBA";
        }

        return $"Day {offering.DayOfWeek}, Period {offering.StartPeriod}-{offering.EndPeriod}";
    }

    private static bool IsStudentContextFailure(AuthException ex)
        => string.Equals(ex.Message, "Current user is not authenticated.", StringComparison.Ordinal)
           || string.Equals(ex.Message, "Current user is not mapped to a student profile.", StringComparison.Ordinal)
           || string.Equals(ex.Message, "Current student profile was not found.", StringComparison.Ordinal);
}
