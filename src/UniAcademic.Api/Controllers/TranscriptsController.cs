using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Transcripts;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Transcripts;
using UniAcademic.Application.Security;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/students/{studentProfileId:guid}/transcript")]
public sealed class TranscriptsController : ControllerBase
{
    private readonly ITranscriptService _transcriptService;

    public TranscriptsController(ITranscriptService transcriptService)
    {
        _transcriptService = transcriptService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Transcripts.View)]
    [HttpGet]
    [ProducesResponseType(typeof(TranscriptModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid studentProfileId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _transcriptService.GetTranscriptAsync(new GetTranscriptQuery
            {
                StudentProfileId = studentProfileId
            }, cancellationToken);

            return Ok(result);
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Student profile was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
