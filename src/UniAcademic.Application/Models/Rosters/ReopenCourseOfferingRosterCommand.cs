namespace UniAcademic.Application.Models.Rosters;

public sealed class ReopenCourseOfferingRosterCommand
{
    public Guid CourseOfferingId { get; set; }

    public string? Reason { get; set; }
}
