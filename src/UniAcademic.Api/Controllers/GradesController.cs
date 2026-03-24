using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.Grades;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Grades;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.Grades;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/grades")]
public sealed class GradesController : ControllerBase
{
    private readonly IGradeService _gradeService;

    public GradesController(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<GradeCategoryListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        var result = await _gradeService.GetListAsync(new GetGradeCategoriesQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GradeCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gradeService.GetByIdAsync(new GetGradeCategoryByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Grade category was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(GradeCategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGradeCategoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gradeService.CreateCategoryAsync(new CreateGradeCategoryCommand
            {
                CourseOfferingId = request.CourseOfferingId,
                Name = request.Name,
                Weight = request.Weight,
                MaxScore = request.MaxScore,
                OrderIndex = request.OrderIndex,
                IsActive = request.IsActive
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, Map(result));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Edit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GradeCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGradeCategoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gradeService.UpdateCategoryAsync(new UpdateGradeCategoryCommand
            {
                Id = id,
                Name = request.Name,
                Weight = request.Weight,
                MaxScore = request.MaxScore,
                OrderIndex = request.OrderIndex,
                IsActive = request.IsActive
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Grade category was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.Grades.Edit)]
    [HttpPut("{id:guid}/entries")]
    [ProducesResponseType(typeof(GradeCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEntries(Guid id, [FromBody] UpdateGradeEntriesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gradeService.UpdateEntriesAsync(new UpdateGradeEntriesCommand
            {
                Id = id,
                Entries = request.Entries.Select(x => new UpdateGradeEntryItemCommand
                {
                    RosterItemId = x.RosterItemId,
                    Score = x.Score,
                    Note = x.Note
                }).ToList()
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Grade category was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static GradeCategoryResponse Map(GradeCategoryModel model)
    {
        return new GradeCategoryResponse
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            CourseOfferingRosterSnapshotId = model.CourseOfferingRosterSnapshotId,
            Name = model.Name,
            Weight = model.Weight,
            MaxScore = model.MaxScore,
            OrderIndex = model.OrderIndex,
            IsActive = model.IsActive,
            EntryCount = model.EntryCount,
            Entries = model.Entries.Select(x => new GradeEntryResponse
            {
                Id = x.Id,
                RosterItemId = x.RosterItemId,
                StudentProfileId = x.StudentProfileId,
                StudentCode = x.StudentCode,
                StudentFullName = x.StudentFullName,
                StudentClassName = x.StudentClassName,
                Score = x.Score,
                Note = x.Note
            }).ToList()
        };
    }

    private static GradeCategoryListItemResponse Map(GradeCategoryListItemModel model)
    {
        return new GradeCategoryListItemResponse
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            Name = model.Name,
            Weight = model.Weight,
            MaxScore = model.MaxScore,
            OrderIndex = model.OrderIndex,
            IsActive = model.IsActive,
            EntryCount = model.EntryCount
        };
    }
}
