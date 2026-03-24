using UniAcademic.Domain.Enums;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class CourseMaterial : AuditableEntity, IAuditableEntity
{
    public Guid CourseOfferingId { get; set; }

    public Guid FileMetadataId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CourseMaterialType MaterialType { get; set; } = CourseMaterialType.Document;

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public CourseOffering? CourseOffering { get; set; }

    public FileMetadata? FileMetadata { get; set; }
}
