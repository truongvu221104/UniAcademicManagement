using Microsoft.AspNetCore.Http;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Api.Models.CourseMaterials;

public sealed class UploadCourseMaterialFormRequest
{
    public Guid CourseOfferingId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CourseMaterialType MaterialType { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    public IFormFile? File { get; set; }
}
