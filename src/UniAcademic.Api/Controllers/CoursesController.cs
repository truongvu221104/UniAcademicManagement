using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Courses;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Courses;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.Courses;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CourseListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? keyword, [FromQuery] Guid? facultyId, [FromQuery] CourseStatus? status, CancellationToken cancellationToken)
    {
        var result = await _courseService.GetListAsync(new GetCoursesQuery
        {
            Keyword = keyword,
            FacultyId = facultyId,
            Status = status
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseService.GetByIdAsync(new GetCourseByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseService.CreateAsync(new CreateCourseCommand
            {
                Code = request.Code,
                Name = request.Name,
                Credits = request.Credits,
                FacultyId = request.FacultyId,
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

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.Edit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseService.UpdateAsync(new UpdateCourseCommand
            {
                Id = id,
                Code = request.Code,
                Name = request.Name,
                Credits = request.Credits,
                FacultyId = request.FacultyId,
                Status = request.Status,
                Description = request.Description
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Courses.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _courseService.DeleteAsync(new DeleteCourseCommand { Id = id }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static CourseResponse Map(CourseModel model)
    {
        return new CourseResponse
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            Credits = model.Credits,
            FacultyId = model.FacultyId,
            FacultyCode = model.FacultyCode,
            FacultyName = model.FacultyName,
            Status = model.Status,
            Description = model.Description
        };
    }

    private static CourseListItemResponse Map(CourseListItemModel model)
    {
        return new CourseListItemResponse
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            Credits = model.Credits,
            FacultyId = model.FacultyId,
            FacultyName = model.FacultyName,
            Status = model.Status
        };
    }
}
