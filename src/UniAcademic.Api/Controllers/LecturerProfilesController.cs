using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.LecturerProfiles;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.LecturerProfiles;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.LecturerProfiles;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LecturerProfilesController : ControllerBase
{
    private readonly ILecturerProfileService _lecturerProfileService;

    public LecturerProfilesController(ILecturerProfileService lecturerProfileService)
    {
        _lecturerProfileService = lecturerProfileService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<LecturerProfileListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? keyword, [FromQuery] Guid? facultyId, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await _lecturerProfileService.GetListAsync(new GetLecturerProfilesQuery
        {
            Keyword = keyword,
            FacultyId = facultyId,
            IsActive = isActive
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LecturerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _lecturerProfileService.GetByIdAsync(new GetLecturerProfileByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Lecturer profile was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(LecturerProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLecturerProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _lecturerProfileService.CreateAsync(new CreateLecturerProfileCommand
            {
                Code = request.Code,
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                FacultyId = request.FacultyId,
                IsActive = request.IsActive,
                Note = request.Note
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, Map(result));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.Edit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LecturerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLecturerProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _lecturerProfileService.UpdateAsync(new UpdateLecturerProfileCommand
            {
                Id = id,
                Code = request.Code,
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                FacultyId = request.FacultyId,
                IsActive = request.IsActive,
                Note = request.Note
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Lecturer profile was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.LecturerProfiles.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _lecturerProfileService.DeleteAsync(new DeleteLecturerProfileCommand { Id = id }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Lecturer profile was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static LecturerProfileResponse Map(LecturerProfileModel model)
    {
        return new LecturerProfileResponse
        {
            Id = model.Id,
            Code = model.Code,
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FacultyId = model.FacultyId,
            FacultyCode = model.FacultyCode,
            FacultyName = model.FacultyName,
            IsActive = model.IsActive,
            Note = model.Note
        };
    }

    private static LecturerProfileListItemResponse Map(LecturerProfileListItemModel model)
    {
        return new LecturerProfileListItemResponse
        {
            Id = model.Id,
            Code = model.Code,
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FacultyId = model.FacultyId,
            FacultyName = model.FacultyName,
            IsActive = model.IsActive
        };
    }
}
