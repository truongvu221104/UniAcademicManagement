using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.Enrollments;

public sealed class GetEnrollmentsQuery
{
    public string? Keyword { get; set; }

    public string? StudentCode { get; set; }

    public string? StudentFullName { get; set; }

    public Guid? StudentProfileId { get; set; }

    public Guid? CourseOfferingId { get; set; }

    public EnrollmentStatus? Status { get; set; }
}
