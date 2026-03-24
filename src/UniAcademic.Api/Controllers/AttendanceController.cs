using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Attendance;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Attendance;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.Attendance;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/attendance")]
public sealed class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AttendanceSessionListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetListAsync(new GetAttendanceSessionsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AttendanceSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.GetByIdAsync(new GetAttendanceSessionByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Attendance session was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(AttendanceSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAttendanceSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.CreateSessionAsync(new CreateAttendanceSessionCommand
            {
                CourseOfferingId = request.CourseOfferingId,
                SessionDate = request.SessionDate,
                SessionNo = request.SessionNo,
                Title = request.Title,
                Note = request.Note
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, Map(result));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Attendance.Edit)]
    [HttpPut("{id:guid}/records")]
    [ProducesResponseType(typeof(AttendanceSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRecords(Guid id, [FromBody] UpdateAttendanceRecordsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.UpdateRecordsAsync(new UpdateAttendanceRecordsCommand
            {
                Id = id,
                Records = request.Records.Select(x => new UpdateAttendanceRecordItemCommand
                {
                    RosterItemId = x.RosterItemId,
                    Status = x.Status,
                    Note = x.Note
                }).ToList()
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Attendance session was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static AttendanceSessionResponse Map(AttendanceSessionModel model)
    {
        return new AttendanceSessionResponse
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            CourseOfferingRosterSnapshotId = model.CourseOfferingRosterSnapshotId,
            SessionDate = model.SessionDate,
            SessionNo = model.SessionNo,
            Title = model.Title,
            Note = model.Note,
            RecordCount = model.RecordCount,
            Records = model.Records.Select(x => new AttendanceRecordResponse
            {
                Id = x.Id,
                RosterItemId = x.RosterItemId,
                StudentProfileId = x.StudentProfileId,
                StudentCode = x.StudentCode,
                StudentFullName = x.StudentFullName,
                StudentClassName = x.StudentClassName,
                Status = x.Status,
                Note = x.Note
            }).ToList()
        };
    }

    private static AttendanceSessionListItemResponse Map(AttendanceSessionListItemModel model)
    {
        return new AttendanceSessionListItemResponse
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            SessionDate = model.SessionDate,
            SessionNo = model.SessionNo,
            Title = model.Title,
            RecordCount = model.RecordCount
        };
    }
}
