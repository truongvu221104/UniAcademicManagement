using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Materials;

public sealed class UploadCourseMaterialCommand
{
    public Guid CourseOfferingId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CourseMaterialType MaterialType { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public Stream FileContent { get; set; } = Stream.Null;
}
