using UniAcademic.Domain.Enums;

namespace UniAcademic.Contracts.CourseOfferings;

public sealed class CreateCourseOfferingRequest
{
    public string Code { get; set; } = string.Empty;

    public Guid CourseId { get; set; }

    public Guid SemesterId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int DayOfWeek { get; set; }

    public int StartPeriod { get; set; }

    public int EndPeriod { get; set; }

    public CourseOfferingStatus Status { get; set; } = CourseOfferingStatus.Active;

    public string? Description { get; set; }
}
