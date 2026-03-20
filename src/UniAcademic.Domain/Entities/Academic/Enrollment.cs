using UniAcademic.Domain.Enums;
using UniAcademic.SharedKernel;

namespace UniAcademic.Domain.Entities.Academic;

public sealed class Enrollment : AuditableEntity, IAuditableEntity
{
    public Guid StudentProfileId { get; set; }

    public Guid CourseOfferingId { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Enrolled;

    public DateTime EnrolledAtUtc { get; set; }

    public DateTime? DroppedAtUtc { get; set; }

    public string? Note { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public StudentProfile? StudentProfile { get; set; }

    public CourseOffering? CourseOffering { get; set; }
}
