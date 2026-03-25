namespace UniAcademic.Contracts.GradeResults;

public sealed class CalculateGradeResultsRequest
{
    public Guid CourseOfferingId { get; set; }

    public decimal PassingScore { get; set; }
}
