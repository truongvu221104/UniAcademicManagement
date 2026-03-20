using UniAcademic.Domain.Enums;

namespace UniAcademic.Contracts.Enrollments;

public sealed class EnrollmentResponse
{
    public Guid Id { get; set; }

    public Guid StudentProfileId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentClassName { get; set; } = string.Empty;

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public EnrollmentStatus Status { get; set; }

    public DateTime EnrolledAtUtc { get; set; }

    public DateTime? DroppedAtUtc { get; set; }

    public string? Note { get; set; }
}
