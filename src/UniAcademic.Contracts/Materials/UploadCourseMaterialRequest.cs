using UniAcademic.Domain.Enums;

namespace UniAcademic.Contracts.Materials;

public sealed class UploadCourseMaterialRequest
{
    public Guid CourseOfferingId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CourseMaterialType MaterialType { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }
}
