using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.CourseOfferings;

public sealed class GetCourseOfferingsQuery
{
    public string? Keyword { get; set; }

    public Guid? CourseId { get; set; }

    public Guid? SemesterId { get; set; }

    public CourseOfferingStatus? Status { get; set; }
}
