using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Application.Abstractions.GradeResults;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.GradeResults;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.GradeResults;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/graderesults")]
public sealed class GradeResultsController : ControllerBase
{
    private readonly IGradeResultService _gradeResultService;

    public GradeResultsController(IGradeResultService gradeResultService)
    {
        _gradeResultService = gradeResultService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.GradeResults.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<GradeResultListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        var result = await _gradeResultService.GetListAsync(new GetGradeResultsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.GradeResults.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GradeResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gradeResultService.GetByIdAsync(new GetGradeResultByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Grade result was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.GradeResults.Calculate)]
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(IReadOnlyCollection<GradeResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Calculate([FromBody] CalculateGradeResultsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gradeResultService.CalculateAsync(new CalculateGradeResultsCommand
            {
                CourseOfferingId = request.CourseOfferingId,
                PassingScore = request.PassingScore
            }, cancellationToken);

            return Ok(result.Select(Map).ToList());
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static GradeResultResponse Map(GradeResultModel model)
    {
        return new GradeResultResponse
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            CourseOfferingRosterSnapshotId = model.CourseOfferingRosterSnapshotId,
            RosterItemId = model.RosterItemId,
            StudentProfileId = model.StudentProfileId,
            StudentCode = model.StudentCode,
            StudentFullName = model.StudentFullName,
            StudentClassName = model.StudentClassName,
            WeightedFinalScore = model.WeightedFinalScore,
            PassingScore = model.PassingScore,
            IsPassed = model.IsPassed,
            CalculatedAtUtc = model.CalculatedAtUtc,
            CalculatedBy = model.CalculatedBy
        };
    }

    private static GradeResultListItemResponse Map(GradeResultListItemModel model)
    {
        return new GradeResultListItemResponse
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            RosterItemId = model.RosterItemId,
            StudentCode = model.StudentCode,
            StudentFullName = model.StudentFullName,
            WeightedFinalScore = model.WeightedFinalScore,
            PassingScore = model.PassingScore,
            IsPassed = model.IsPassed,
            CalculatedAtUtc = model.CalculatedAtUtc
        };
    }
}
