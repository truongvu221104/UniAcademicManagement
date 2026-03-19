using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Semesters;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Semesters;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.Semesters;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SemestersController : ControllerBase
{
    private readonly ISemesterService _semesterService;

    public SemestersController(ISemesterService semesterService)
    {
        _semesterService = semesterService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<SemesterListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? keyword, [FromQuery] string? academicYear, [FromQuery] int? termNo, [FromQuery] SemesterStatus? status, CancellationToken cancellationToken)
    {
        var result = await _semesterService.GetListAsync(new GetSemestersQuery
        {
            Keyword = keyword,
            AcademicYear = academicYear,
            TermNo = termNo,
            Status = status
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SemesterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _semesterService.GetByIdAsync(new GetSemesterByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Semester was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(SemesterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSemesterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _semesterService.CreateAsync(new CreateSemesterCommand
            {
                Code = request.Code,
                Name = request.Name,
                AcademicYear = request.AcademicYear,
                TermNo = request.TermNo,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
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

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.Edit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SemesterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSemesterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _semesterService.UpdateAsync(new UpdateSemesterCommand
            {
                Id = id,
                Code = request.Code,
                Name = request.Name,
                AcademicYear = request.AcademicYear,
                TermNo = request.TermNo,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status,
                Description = request.Description
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Semester was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Semesters.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _semesterService.DeleteAsync(new DeleteSemesterCommand { Id = id }, cancellationToken);
            return NoContent();
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Semester was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static SemesterResponse Map(SemesterModel model)
    {
        return new SemesterResponse
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            AcademicYear = model.AcademicYear,
            TermNo = model.TermNo,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Status = model.Status,
            Description = model.Description
        };
    }

    private static SemesterListItemResponse Map(SemesterListItemModel model)
    {
        return new SemesterListItemResponse
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            AcademicYear = model.AcademicYear,
            TermNo = model.TermNo,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Status = model.Status
        };
    }
}
