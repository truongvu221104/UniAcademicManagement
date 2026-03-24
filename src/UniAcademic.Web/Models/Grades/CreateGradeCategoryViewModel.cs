using System.ComponentModel.DataAnnotations;

namespace UniAcademic.Web.Models.Grades;

public sealed class CreateGradeCategoryViewModel
{
    [Required]
    public Guid CourseOfferingId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100")]
    public decimal Weight { get; set; }

    [Range(typeof(decimal), "0.01", "999999")]
    public decimal MaxScore { get; set; }

    [Range(0, int.MaxValue)]
    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;
}
