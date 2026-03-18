using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Faculties;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Faculties;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.Faculties;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FacultiesController : ControllerBase
{
    private readonly IFacultyService _facultyService;

    public FacultiesController(IFacultyService facultyService)
    {
        _facultyService = facultyService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<FacultyListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? keyword, [FromQuery] UniAcademic.Domain.Enums.FacultyStatus? status, CancellationToken cancellationToken)
    {
        var result = await _facultyService.GetListAsync(new GetFacultiesQuery
        {
            Keyword = keyword,
            Status = status
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FacultyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _facultyService.GetByIdAsync(new GetFacultyByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Faculty was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(FacultyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFacultyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _facultyService.CreateAsync(new CreateFacultyCommand
            {
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                Status = request.Status
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, Map(result));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.Edit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FacultyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFacultyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _facultyService.UpdateAsync(new UpdateFacultyCommand
            {
                Id = id,
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                Status = request.Status
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Faculty was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Faculties.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _facultyService.DeleteAsync(new DeleteFacultyCommand { Id = id }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Faculty was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static FacultyResponse Map(FacultyModel model)
    {
        return new FacultyResponse
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            Description = model.Description,
            Status = model.Status
        };
    }

    private static FacultyListItemResponse Map(FacultyListItemModel model)
    {
        return new FacultyListItemResponse
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            Status = model.Status
        };
    }
}
