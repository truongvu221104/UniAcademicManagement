namespace UniAcademic.Application.Models.Grades;

public sealed class CreateGradeCategoryCommand
{
    public Guid CourseOfferingId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public decimal MaxScore { get; set; }

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;
}
