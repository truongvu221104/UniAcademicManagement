using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Enrollments;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Enrollments;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.Enrollments;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IAuthorizationService _authorizationService;

    public EnrollmentsController(IEnrollmentService enrollmentService, IAuthorizationService authorizationService)
    {
        _enrollmentService = enrollmentService;
        _authorizationService = authorizationService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<EnrollmentListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? keyword,
        [FromQuery] string? studentCode,
        [FromQuery] string? studentFullName,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] Guid? courseOfferingId,
        [FromQuery] EnrollmentStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.GetListAsync(new GetEnrollmentsQuery
        {
            Keyword = keyword,
            StudentCode = studentCode,
            StudentFullName = studentFullName,
            StudentProfileId = studentProfileId,
            CourseOfferingId = courseOfferingId,
            Status = status
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _enrollmentService.GetByIdAsync(new GetEnrollmentByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Enrollment was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.IsOverride)
            {
                var authorizationResult = await _authorizationService.AuthorizeAsync(
                    User,
                    null,
                    PermissionConstants.BuildPolicy(PermissionConstants.Enrollments.Override));

                if (!authorizationResult.Succeeded)
                {
                    return Forbid();
                }
            }

            var result = await _enrollmentService.EnrollAsync(new EnrollStudentCommand
            {
                StudentProfileId = request.StudentProfileId,
                CourseOfferingId = request.CourseOfferingId,
                Note = request.Note,
                IsOverride = request.IsOverride,
                OverrideReason = request.OverrideReason
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, Map(result));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Enrollments.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _enrollmentService.DropAsync(new DropEnrollmentCommand { Id = id }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Enrollment was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static EnrollmentResponse Map(EnrollmentModel model)
    {
        return new EnrollmentResponse
        {
            Id = model.Id,
            StudentProfileId = model.StudentProfileId,
            StudentCode = model.StudentCode,
            StudentFullName = model.StudentFullName,
            StudentClassName = model.StudentClassName,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseCode = model.CourseCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            Status = model.Status,
            EnrolledAtUtc = model.EnrolledAtUtc,
            DroppedAtUtc = model.DroppedAtUtc,
            Note = model.Note
        };
    }

    private static EnrollmentListItemResponse Map(EnrollmentListItemModel model)
    {
        return new EnrollmentListItemResponse
        {
            Id = model.Id,
            StudentProfileId = model.StudentProfileId,
            StudentCode = model.StudentCode,
            StudentFullName = model.StudentFullName,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            Status = model.Status,
            EnrolledAtUtc = model.EnrolledAtUtc
        };
    }
}
