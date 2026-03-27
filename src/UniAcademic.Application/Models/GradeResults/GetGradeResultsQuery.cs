namespace UniAcademic.Application.Models.GradeResults;

public sealed class GetGradeResultsQuery
{
    public string? StudentCode { get; set; }

    public string? StudentFullName { get; set; }

    public Guid? CourseOfferingId { get; set; }
}
