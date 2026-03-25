using System.ComponentModel.DataAnnotations;

namespace UniAcademic.Web.Models.GradeResults;

public sealed class CalculateGradeResultsViewModel
{
    [Required]
    public Guid CourseOfferingId { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal PassingScore { get; set; } = 50m;
}
