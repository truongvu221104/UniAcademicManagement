using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class LecturerAssignment : AuditableEntity, IAuditableEntity
{
    public Guid CourseOfferingId { get; set; }

    public Guid LecturerProfileId { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public CourseOffering? CourseOffering { get; set; }

    public LecturerProfile? LecturerProfile { get; set; }
}
