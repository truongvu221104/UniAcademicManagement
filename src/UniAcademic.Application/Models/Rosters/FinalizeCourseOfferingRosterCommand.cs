namespace UniAcademic.Application.Models.Rosters;

public sealed class FinalizeCourseOfferingRosterCommand
{
    public Guid CourseOfferingId { get; set; }

    public string? Note { get; set; }
}
