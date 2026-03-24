using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Materials;

public sealed class UpdateCourseMaterialCommand
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CourseMaterialType MaterialType { get; set; }

    public int SortOrder { get; set; }
}
