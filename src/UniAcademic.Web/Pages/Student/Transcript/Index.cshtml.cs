using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Transcripts;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Transcripts;
using UniAcademic.Application.Security;

namespace UniAcademic.Web.Pages.Student.Transcript;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Transcripts.View)]
public sealed class IndexModel : PageModel
{
    private readonly ICurrentStudentContext _currentStudentContext;
    private readonly ITranscriptService _transcriptService;

    public IndexModel(
        ICurrentStudentContext currentStudentContext,
        ITranscriptService transcriptService)
    {
        _currentStudentContext = currentStudentContext;
        _transcriptService = transcriptService;
    }

    public TranscriptModel Transcript { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var studentProfileId = await _currentStudentContext.GetRequiredStudentProfileIdAsync(cancellationToken);
            Transcript = await _transcriptService.GetTranscriptAsync(new GetTranscriptQuery
            {
                StudentProfileId = studentProfileId
            }, cancellationToken);

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
