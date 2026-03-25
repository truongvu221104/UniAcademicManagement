namespace UniAcademic.Application.Models.GradeResults;

public sealed class CalculateGradeResultsCommand
{
    public Guid CourseOfferingId { get; set; }

    public decimal PassingScore { get; set; }
}
