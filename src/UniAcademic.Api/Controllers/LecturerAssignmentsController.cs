using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.LecturerAssignments;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.LecturerAssignments;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.LecturerAssignments;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LecturerAssignmentsController : ControllerBase
{
    private readonly ILecturerAssignmentService _lecturerAssignmentService;

    public LecturerAssignmentsController(ILecturerAssignmentService lecturerAssignmentService)
    {
        _lecturerAssignmentService = lecturerAssignmentService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerAssignments.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<LecturerAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] Guid? courseOfferingId, [FromQuery] Guid? lecturerProfileId, CancellationToken cancellationToken)
    {
        var result = await _lecturerAssignmentService.GetListAsync(new GetLecturerAssignmentsQuery
        {
            CourseOfferingId = courseOfferingId,
            LecturerProfileId = lecturerProfileId
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerAssignments.Assign)]
    [HttpPost]
    [ProducesResponseType(typeof(LecturerAssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] AssignLecturerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _lecturerAssignmentService.AssignAsync(new AssignLecturerCommand
            {
                CourseOfferingId = request.CourseOfferingId,
                LecturerProfileId = request.LecturerProfileId,
                IsPrimary = request.IsPrimary
            }, cancellationToken);

            return CreatedAtAction(nameof(GetList), new { courseOfferingId = result.CourseOfferingId }, Map(result));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerAssignments.Unassign)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _lecturerAssignmentService.UnassignAsync(new UnassignLecturerCommand { Id = id }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Lecturer assignment was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static LecturerAssignmentResponse Map(LecturerAssignmentModel model)
    {
        return new LecturerAssignmentResponse
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            LecturerProfileId = model.LecturerProfileId,
            LecturerCode = model.LecturerCode,
            LecturerFullName = model.LecturerFullName,
            FacultyName = model.FacultyName,
            IsPrimary = model.IsPrimary,
            AssignedAtUtc = model.AssignedAtUtc
        };
    }
}
