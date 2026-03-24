using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Materials;

public sealed class CourseMaterialListItemModel
{
    public Guid Id { get; set; }

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public CourseMaterialType MaterialType { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public DateTime UploadedAtUtc { get; set; }
}
