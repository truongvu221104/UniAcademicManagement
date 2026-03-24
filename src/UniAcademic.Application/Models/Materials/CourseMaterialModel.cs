using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Materials;

public sealed class CourseMaterialModel
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public Guid FileMetadataId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CourseMaterialType MaterialType { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public DateTime UploadedAtUtc { get; set; }

    public string UploadedBy { get; set; } = string.Empty;
}
