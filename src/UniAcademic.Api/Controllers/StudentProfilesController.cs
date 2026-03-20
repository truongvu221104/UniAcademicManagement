using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.StudentProfiles;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.StudentProfiles;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.StudentProfiles;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StudentProfilesController : ControllerBase
{
    private readonly IStudentProfileService _studentProfileService;

    public StudentProfilesController(IStudentProfileService studentProfileService)
    {
        _studentProfileService = studentProfileService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<StudentProfileListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? keyword, [FromQuery] Guid? studentClassId, [FromQuery] StudentGender? gender, [FromQuery] StudentProfileStatus? status, CancellationToken cancellationToken)
    {
        var result = await _studentProfileService.GetListAsync(new GetStudentProfilesQuery
        {
            Keyword = keyword,
            StudentClassId = studentClassId,
            Gender = gender,
            Status = status
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudentProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _studentProfileService.GetByIdAsync(new GetStudentProfileByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Student profile was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(StudentProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStudentProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _studentProfileService.CreateAsync(new CreateStudentProfileCommand
            {
                Code = request.StudentCode,
                FullName = request.FullName,
                StudentClassId = request.StudentClassId,
                Email = request.Email,
                Phone = request.Phone,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Address = request.Address,
                Status = request.Status,
                Note = request.Note
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, Map(result));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.Edit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StudentProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _studentProfileService.UpdateAsync(new UpdateStudentProfileCommand
            {
                Id = id,
                Code = request.StudentCode,
                FullName = request.FullName,
                StudentClassId = request.StudentClassId,
                Email = request.Email,
                Phone = request.Phone,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Address = request.Address,
                Status = request.Status,
                Note = request.Note
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Student profile was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentProfiles.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _studentProfileService.DeleteAsync(new DeleteStudentProfileCommand { Id = id }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Student profile was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static StudentProfileResponse Map(StudentProfileModel model)
    {
        return new StudentProfileResponse
        {
            Id = model.Id,
            StudentCode = model.StudentCode,
            FullName = model.FullName,
            StudentClassId = model.StudentClassId,
            StudentClassCode = model.StudentClassCode,
            StudentClassName = model.StudentClassName,
            Email = model.Email,
            Phone = model.Phone,
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender,
            Address = model.Address,
            Status = model.Status,
            Note = model.Note
        };
    }

    private static StudentProfileListItemResponse Map(StudentProfileListItemModel model)
    {
        return new StudentProfileListItemResponse
        {
            Id = model.Id,
            StudentCode = model.StudentCode,
            FullName = model.FullName,
            StudentClassId = model.StudentClassId,
            StudentClassName = model.StudentClassName,
            Email = model.Email,
            Phone = model.Phone,
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender,
            Status = model.Status
        };
    }
}
