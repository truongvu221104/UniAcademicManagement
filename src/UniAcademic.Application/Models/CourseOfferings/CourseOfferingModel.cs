using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Models.CourseOfferings;

public sealed class CourseOfferingModel
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public Guid CourseId { get; set; }

    public string CourseCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public Guid SemesterId { get; set; }

    public string SemesterCode { get; set; } = string.Empty;

    public string SemesterName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public CourseOfferingStatus Status { get; set; }

    public string? Description { get; set; }
}
