using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Rosters;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Rosters;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.Rosters;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/courseofferings/{courseOfferingId:guid}/roster")]
public sealed class CourseOfferingRostersController : ControllerBase
{
    private readonly ICourseOfferingRosterService _courseOfferingRosterService;

    public CourseOfferingRostersController(ICourseOfferingRosterService courseOfferingRosterService)
    {
        _courseOfferingRosterService = courseOfferingRosterService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferingRosters.View)]
    [HttpGet]
    [ProducesResponseType(typeof(CourseOfferingRosterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseOfferingRosterService.GetByCourseOfferingIdAsync(new GetCourseOfferingRosterQuery
            {
                CourseOfferingId = courseOfferingId
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course offering was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferingRosters.Finalize)]
    [HttpPost("finalize")]
    [ProducesResponseType(typeof(CourseOfferingRosterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Finalize(Guid courseOfferingId, [FromBody] FinalizeCourseOfferingRosterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseOfferingRosterService.FinalizeAsync(new FinalizeCourseOfferingRosterCommand
            {
                CourseOfferingId = courseOfferingId,
                Note = request.Note
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course offering was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static CourseOfferingRosterResponse Map(CourseOfferingRosterModel model)
    {
        return new CourseOfferingRosterResponse
        {
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            IsFinalized = model.IsFinalized,
            FinalizedAtUtc = model.FinalizedAtUtc,
            FinalizedBy = model.FinalizedBy,
            ItemCount = model.ItemCount,
            Note = model.Note,
            Items = model.Items.Select(x => new CourseOfferingRosterItemResponse
            {
                EnrollmentId = x.EnrollmentId,
                StudentProfileId = x.StudentProfileId,
                StudentCode = x.StudentCode,
                StudentFullName = x.StudentFullName,
                StudentClassName = x.StudentClassName,
                CourseOfferingCode = x.CourseOfferingCode,
                CourseCode = x.CourseCode,
                CourseName = x.CourseName,
                SemesterName = x.SemesterName
            }).ToList()
        };
    }
}
