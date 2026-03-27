using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.CourseOfferings;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.CourseOfferings;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.CourseOfferings;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CourseOfferingsController : ControllerBase
{
    private readonly ICourseOfferingService _courseOfferingService;

    public CourseOfferingsController(ICourseOfferingService courseOfferingService)
    {
        _courseOfferingService = courseOfferingService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CourseOfferingListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? keyword, [FromQuery] Guid? courseId, [FromQuery] Guid? semesterId, [FromQuery] CourseOfferingStatus? status, CancellationToken cancellationToken)
    {
        var result = await _courseOfferingService.GetListAsync(new GetCourseOfferingsQuery
        {
            Keyword = keyword,
            CourseId = courseId,
            SemesterId = semesterId,
            Status = status
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourseOfferingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseOfferingService.GetByIdAsync(new GetCourseOfferingByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course offering was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(CourseOfferingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCourseOfferingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseOfferingService.CreateAsync(new CreateCourseOfferingCommand
            {
                Code = request.Code,
                CourseId = request.CourseId,
                SemesterId = request.SemesterId,
                DisplayName = request.DisplayName,
                Capacity = request.Capacity,
                DayOfWeek = request.DayOfWeek,
                StartPeriod = request.StartPeriod,
                EndPeriod = request.EndPeriod,
                Status = request.Status,
                Description = request.Description
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, Map(result));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.Edit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CourseOfferingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseOfferingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseOfferingService.UpdateAsync(new UpdateCourseOfferingCommand
            {
                Id = id,
                Code = request.Code,
                CourseId = request.CourseId,
                SemesterId = request.SemesterId,
                DisplayName = request.DisplayName,
                Capacity = request.Capacity,
                DayOfWeek = request.DayOfWeek,
                StartPeriod = request.StartPeriod,
                EndPeriod = request.EndPeriod,
                Status = request.Status,
                Description = request.Description
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

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseOfferings.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _courseOfferingService.DeleteAsync(new DeleteCourseOfferingCommand { Id = id }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course offering was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static CourseOfferingResponse Map(CourseOfferingModel model)
    {
        return new CourseOfferingResponse
        {
            Id = model.Id,
            Code = model.Code,
            CourseId = model.CourseId,
            CourseCode = model.CourseCode,
            CourseName = model.CourseName,
            SemesterId = model.SemesterId,
            SemesterCode = model.SemesterCode,
            SemesterName = model.SemesterName,
            DisplayName = model.DisplayName,
            EnrolledCount = model.EnrolledCount,
            Capacity = model.Capacity,
            DayOfWeek = model.DayOfWeek,
            StartPeriod = model.StartPeriod,
            EndPeriod = model.EndPeriod,
            Status = model.Status,
            Description = model.Description
        };
    }

    private static CourseOfferingListItemResponse Map(CourseOfferingListItemModel model)
    {
        return new CourseOfferingListItemResponse
        {
            Id = model.Id,
            Code = model.Code,
            CourseId = model.CourseId,
            CourseCode = model.CourseCode,
            CourseName = model.CourseName,
            SemesterId = model.SemesterId,
            SemesterName = model.SemesterName,
            DisplayName = model.DisplayName,
            EnrolledCount = model.EnrolledCount,
            Capacity = model.Capacity,
            DayOfWeek = model.DayOfWeek,
            StartPeriod = model.StartPeriod,
            EndPeriod = model.EndPeriod,
            Status = model.Status
        };
    }
}
