using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Enrollments;

public sealed class EnrollmentListItemModel
{
    public Guid Id { get; set; }

    public Guid StudentProfileId { get; set; }

    public string StudentCode { get; set; } = string.Empty;

    public string StudentFullName { get; set; } = string.Empty;

    public Guid CourseOfferingId { get; set; }

    public string CourseOfferingCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public EnrollmentStatus Status { get; set; }

    public DateTime EnrolledAtUtc { get; set; }
}
