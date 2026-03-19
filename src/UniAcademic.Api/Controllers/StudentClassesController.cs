using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.StudentClasses;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.StudentClasses;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.StudentClasses;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StudentClassesController : ControllerBase
{
    private readonly IStudentClassService _studentClassService;

    public StudentClassesController(IStudentClassService studentClassService)
    {
        _studentClassService = studentClassService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<StudentClassListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? keyword, [FromQuery] Guid? facultyId, [FromQuery] int? intakeYear, [FromQuery] StudentClassStatus? status, CancellationToken cancellationToken)
    {
        var result = await _studentClassService.GetListAsync(new GetStudentClassesQuery
        {
            Keyword = keyword,
            FacultyId = facultyId,
            IntakeYear = intakeYear,
            Status = status
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudentClassResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _studentClassService.GetByIdAsync(new GetStudentClassByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Student class was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(StudentClassResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStudentClassRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _studentClassService.CreateAsync(new CreateStudentClassCommand
            {
                Code = request.Code,
                Name = request.Name,
                FacultyId = request.FacultyId,
                IntakeYear = request.IntakeYear,
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

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.Edit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StudentClassResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentClassRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _studentClassService.UpdateAsync(new UpdateStudentClassCommand
            {
                Id = id,
                Code = request.Code,
                Name = request.Name,
                FacultyId = request.FacultyId,
                IntakeYear = request.IntakeYear,
                Status = request.Status,
                Description = request.Description
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Student class was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.StudentClasses.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _studentClassService.DeleteAsync(new DeleteStudentClassCommand { Id = id }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Student class was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static StudentClassResponse Map(StudentClassModel model)
    {
        return new StudentClassResponse
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            FacultyId = model.FacultyId,
            FacultyCode = model.FacultyCode,
            FacultyName = model.FacultyName,
            IntakeYear = model.IntakeYear,
            Status = model.Status,
            Description = model.Description
        };
    }

    private static StudentClassListItemResponse Map(StudentClassListItemModel model)
    {
        return new StudentClassListItemResponse
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            FacultyId = model.FacultyId,
            FacultyName = model.FacultyName,
            IntakeYear = model.IntakeYear,
            Status = model.Status
        };
    }
}
