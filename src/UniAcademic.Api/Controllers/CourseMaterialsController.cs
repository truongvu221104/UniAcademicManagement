using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniAcademic.Api.Models.CourseMaterials;
using UniAcademic.Application.Abstractions.Materials;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Materials;
using UniAcademic.Application.Security;
using UniAcademic.Contracts.Materials;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Api.Controllers;

[ApiController]
[Route("api/coursematerials")]
public sealed class CourseMaterialsController : ControllerBase
{
    private readonly ICourseMaterialService _courseMaterialService;

    public CourseMaterialsController(ICourseMaterialService courseMaterialService)
    {
        _courseMaterialService = courseMaterialService;
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.View)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CourseMaterialListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] Guid? courseOfferingId, CancellationToken cancellationToken)
    {
        var result = await _courseMaterialService.GetListAsync(new GetCourseMaterialsQuery
        {
            CourseOfferingId = courseOfferingId
        }, cancellationToken);

        return Ok(result.Select(Map).ToList());
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.View)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourseMaterialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseMaterialService.GetByIdAsync(new GetCourseMaterialByIdQuery { Id = id }, cancellationToken);
            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course material was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Create)]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CourseMaterialResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload([FromForm] UploadCourseMaterialFormRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return BadRequest(new { message = "Material file is required." });
        }

        try
        {
            await using var stream = request.File.OpenReadStream();
            var result = await _courseMaterialService.UploadAsync(new UploadCourseMaterialCommand
            {
                CourseOfferingId = request.CourseOfferingId,
                Title = request.Title,
                Description = request.Description,
                MaterialType = request.MaterialType,
                SortOrder = request.SortOrder,
                IsPublished = request.IsPublished,
                OriginalFileName = request.File.FileName,
                ContentType = request.File.ContentType,
                SizeInBytes = request.File.Length,
                FileContent = stream
            }, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, Map(result));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Edit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CourseMaterialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseMaterialRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseMaterialService.UpdateAsync(new UpdateCourseMaterialCommand
            {
                Id = id,
                Title = request.Title,
                Description = request.Description,
                MaterialType = request.MaterialType,
                SortOrder = request.SortOrder
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course material was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Edit)]
    [HttpPut("{id:guid}/publish-state")]
    [ProducesResponseType(typeof(CourseMaterialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPublishState(Guid id, [FromBody] SetCourseMaterialPublishStateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _courseMaterialService.SetPublishStateAsync(new SetCourseMaterialPublishStateCommand
            {
                Id = id,
                IsPublished = request.IsPublished
            }, cancellationToken);

            return Ok(Map(result));
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course material was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Policy = PermissionConstants.PolicyPrefix + PermissionConstants.CourseMaterials.Download)]
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await using var result = await _courseMaterialService.DownloadAsync(new DownloadCourseMaterialQuery { Id = id }, cancellationToken);
            await using var content = new MemoryStream();
            await result.Content.CopyToAsync(content, cancellationToken);
            return File(content.ToArray(), result.ContentType, result.FileName);
        }
        catch (AuthException ex) when (string.Equals(ex.Message, "Course material was not found.", StringComparison.Ordinal))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static CourseMaterialResponse Map(CourseMaterialModel model)
    {
        return new CourseMaterialResponse
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            FileMetadataId = model.FileMetadataId,
            Title = model.Title,
            Description = model.Description,
            MaterialType = model.MaterialType,
            SortOrder = model.SortOrder,
            IsPublished = model.IsPublished,
            OriginalFileName = model.OriginalFileName,
            ContentType = model.ContentType,
            SizeInBytes = model.SizeInBytes,
            UploadedAtUtc = model.UploadedAtUtc,
            UploadedBy = model.UploadedBy
        };
    }

    private static CourseMaterialListItemResponse Map(CourseMaterialListItemModel model)
    {
        return new CourseMaterialListItemResponse
        {
            Id = model.Id,
            CourseOfferingId = model.CourseOfferingId,
            CourseOfferingCode = model.CourseOfferingCode,
            CourseName = model.CourseName,
            SemesterName = model.SemesterName,
            Title = model.Title,
            MaterialType = model.MaterialType,
            SortOrder = model.SortOrder,
            IsPublished = model.IsPublished,
            OriginalFileName = model.OriginalFileName,
            ContentType = model.ContentType,
            SizeInBytes = model.SizeInBytes,
            UploadedAtUtc = model.UploadedAtUtc
        };
    }
}
