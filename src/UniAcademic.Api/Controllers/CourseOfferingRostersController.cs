using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.ExamHandoff;
using UniAcademic.Application.Abstractions.Rosters;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.ExamHandoff;
using UniAcademic.Application.Models.Rosters;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.Rosters;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/courseofferings/{courseOfferingId:guid}/roster")]
public sealed class CourseOfferingRostersController : ControllerBase
{
    private readonly ICourseOfferingRosterService _courseOfferingRosterService;
    private readonly IExamHandoffService _examHandoffService;

    public CourseOfferingRostersController(
        ICourseOfferingRosterService courseOfferingRosterService,
        IExamHandoffService examHandoffService)
    {
        _courseOfferingRosterService = courseOfferingRosterService;
        _examHandoffService = examHandoffService;
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

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferingRosters.RetryHandoff)]
    [HttpPost("handoff")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RetryHandoff(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        await _examHandoffService.RetryHandoffAsync(courseOfferingId, cancellationToken);
        return Accepted();
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferingRosters.View)]
    [HttpGet("handoff/status")]
    [ProducesResponseType(typeof(ExamHandoffStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHandoffStatus(Guid courseOfferingId, CancellationToken cancellationToken)
    {
        var result = await _examHandoffService.GetLatestStatusAsync(courseOfferingId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { message = "Exam handoff status was not found." });
        }

        return Ok(Map(result));
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

    private static ExamHandoffStatusResponse Map(ExamHandoffLogModel model)
    {
        return new ExamHandoffStatusResponse
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            RosterSnapshotId = model.RosterSnapshotId,
            Status = model.Status,
            SentAtUtc = model.SentAtUtc,
            ResponseCode = model.ResponseCode,
            ErrorMessage = model.ErrorMessage
        };
    }
}
