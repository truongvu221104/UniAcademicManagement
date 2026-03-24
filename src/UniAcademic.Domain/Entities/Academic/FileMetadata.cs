using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class FileMetadata : AuditableEntity, IAuditableEntity
{
    public string OriginalFileName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public DateTime UploadedAtUtc { get; set; }

    public string UploadedBy { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public CourseMaterial? CourseMaterial { get; set; }
}
