using UniAcademic.Domain.Enums;

namespace UniAcademic.Contracts.Materials;

public sealed class UpdateCourseMaterialRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CourseMaterialType MaterialType { get; set; }

    public int SortOrder { get; set; }
}
