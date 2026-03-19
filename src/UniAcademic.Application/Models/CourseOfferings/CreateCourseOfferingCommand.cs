using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.CourseOfferings;

public sealed class CreateCourseOfferingCommand
{
    public string Code { get; set; } = string.Empty;

    public Guid CourseId { get; set; }

    public Guid SemesterId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public CourseOfferingStatus Status { get; set; } = CourseOfferingStatus.Active;

    public string? Description { get; set; }
}
